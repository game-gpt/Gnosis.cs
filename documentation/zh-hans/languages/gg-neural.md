# gg-neural 神经着色器语言指南

本文档介绍 gg 引擎中神经着色器的编程模型——如何利用现代 GPU 的深度学习算子与图形渲染能力，在 gg-shader 中直接定义和执行神经网络推理。

## 设计动机

现代 GPU（NVIDIA Ampere+/AMD CDNA+/Apple M 系列）同时具备**图形渲染管线**和**深度学习加速器（Tensor Core / Matrix Core / AMX）**。传统做法将二者割裂：渲染走图形 API，推理走深度学习框架，中间通过 CPU 拷贝衔接——这带来了不可接受的延迟和带宽浪费。

gg-neural 的核心主张：**深度学习算子就是一类特殊的着色器原语，Tensor Core 就是一种特殊的计算单元**。将神经网络推理直接嵌入渲染管线，消除 CPU 中转，实现帧内推理。

### 问题：IR on IR on IR

传统方案引入 ONNX 等通用 IR 作为中间层，导致：

```
gg-shader IR → ONNX IR → TensorRT IR → GPU Kernel
```

每一层 IR 都有自己的优化器、类型系统和语义假设，层间映射不可避免地丢失信息，产生"套娃优化"问题——上层 IR 的优化决策可能被下层 IR 推翻，甚至引入正确性风险。

gg-neural 的解法：**单一 IR 路径**。神经网络原语直接作为 gg-shader IR 的一等公民，编译器从源码到 Tensor Core 指令只经过一层 IR 变换：

```
gg-shader 源码 → gg-shader IR → Tensor Core 调度 / SPIR-V Cooperative Matrix
```

## 编程模型

### neural 语句

`neural` 是 gg-shader 中定义神经网络层的一等语句，与 `micro` 函数平级：

```rust
# 定义一个线性层
neural LinearLayer<in_dim: u32, out_dim: u32> {
    weight: tensor<f32, [out_dim, in_dim]>,
    bias: tensor<f32, [out_dim]>,

    forward(input: vec<in_dim, f32>) -> vec<out_dim, f32> {
        return matmul(weight, input) + bias;
    }
}
```

### 核心概念

| 概念 | 描述 |
| :--- | :--- |
| `neural` 块 | 定义一个可微分计算单元，包含参数声明和前向传播逻辑 |
| `tensor<T, [dims]>` | 张量类型，声明权重/偏置等参数 |
| `forward()` | 前向传播函数，定义计算图 |
| `matmul` | 矩阵乘法，编译器映射为 Tensor Core 指令 |

### 与 micro 的关系

`neural` 块和 `micro` 函数是互补的：

| 维度 | `micro` 函数 | `neural` 块 |
| :--- | :--- | :--- |
| 语义 | 逐元素/逐顶点/逐像素计算 | 批量矩阵/张量计算 |
| 执行单元 | CUDA Core / Stream Processor | Tensor Core / Matrix Core |
| 数据粒度 | 标量、向量 | 矩阵分块、张量 |
| 典型用途 | 光照计算、纹理采样 | MLP 推理、卷积、注意力 |

`neural` 块可以在 `micro` 函数中被调用，二者在同一渲染帧内无缝协作：

```rust
# 在片段着色器中调用 NeRF 的 MLP
[Fragment]
micro ps_main(input: VertexOutput) -> vec4<f32> {
    let nerf_color = nerf_mlp.forward(vec.from(input.world_pos, input.view_dir));
    return nerf_color;
}
```

## 内置神经原语

gg-neural 提供以下内置原语，编译器直接映射到 GPU 深度学习指令：

### 线性层

```rust
neural LinearLayer<in_dim: u32, out_dim: u32> {
    weight: tensor<f32, [out_dim, in_dim]>,
    bias: tensor<f32, [out_dim]>,

    forward(input: vec<in_dim, f32>) -> vec<out_dim, f32> {
        return matmul(weight, input) + bias;
    }
}
```

### 卷积层

```rust
neural Conv2D<channels_in: u32, channels_out: u32, kernel_size: u32, stride: u32 = 1, padding: u32 = 0> {
    weight: tensor<f32, [channels_out, channels_in, kernel_size, kernel_size]>,
    bias: tensor<f32, [channels_out]>,

    forward(input: tensor<f32, [channels_in, height, width]>) -> tensor<f32, [channels_out, out_height, out_width]> {
        return conv2d(weight, input, stride, padding) + bias;
    }
}
```

### 激活函数

激活函数以 `micro` 函数形式提供，作用于 `neural` 块的输出：

```rust
# 内置激活函数
micro relu(x: f32) -> f32 { return max(0.0, x); }
micro sigmoid(x: f32) -> f32 { return 1.0 / (1.0 + exp(-x)); }
micro gelu(x: f32) -> f32 { return x * 0.5 * (1.0 + erf(x / sqrt(2.0))); }
micro silu(x: f32) -> f32 { return x * sigmoid(x); }

# 向量化版本（编译器自动向量化）
micro relu_vec< N: u32>(x: vec<N, f32>) -> vec<N, f32>;
micro sigmoid_vec< N: u32>(x: vec<N, f32>) -> vec<N, f32>;
```

### 多层感知机（MLP）

```rust
neural MLP<hidden_dim: u32, output_dim: u32, num_layers: u32> {
    # 元编程生成多层权重
    <% for (int i = 0; i < num_layers; i++) { %>
        let layer_<%= i %>_weight: tensor<f32, [<%= i == num_layers - 1 ? "output_dim" : "hidden_dim" %>, <%= i == 0 ? "input_dim" : "hidden_dim" %>]>;
        let layer_<%= i %>_bias: tensor<f32, [<%= i == num_layers - 1 ? "output_dim" : "hidden_dim" %>]>;
    <% } %>

    forward(input: vec<input_dim, f32>) -> vec<output_dim, f32> {
        var x = input;
        <% for (int i = 0; i < num_layers; i++) { %>
            x = matmul(layer_<%= i %>_weight, x) + layer_<%= i %>_bias;
            <% if (i < num_layers - 1) { %>
                x = relu_vec(x);
            <% } %>
        <% } %>
        return x;
    }
}
```

### 注意力机制

```rust
neural SelfAttention<embed_dim: u32, num_heads: u32> {
    let head_dim: u32 = embed_dim / num_heads;

    q_proj_weight: tensor<f32, [embed_dim, embed_dim]>,
    q_proj_bias: tensor<f32, [embed_dim]>,
    k_proj_weight: tensor<f32, [embed_dim, embed_dim]>,
    k_proj_bias: tensor<f32, [embed_dim]>,
    v_proj_weight: tensor<f32, [embed_dim, embed_dim]>,
    v_proj_bias: tensor<f32, [embed_dim]>,
    out_proj_weight: tensor<f32, [embed_dim, embed_dim]>,
    out_proj_bias: tensor<f32, [embed_dim]>,

    forward(x: tensor<f32, [seq_len, embed_dim]>) -> tensor<f32, [seq_len, embed_dim]> {
        let q = matmul(x, q_proj_weight) + q_proj_bias;
        let k = matmul(x, k_proj_weight) + k_proj_bias;
        let v = matmul(x, v_proj_weight) + v_proj_bias;

        # 多头分割
        let q_heads = reshape(q, [num_heads, seq_len, head_dim]);
        let k_heads = reshape(k, [num_heads, seq_len, head_dim]);
        let v_heads = reshape(v, [num_heads, seq_len, head_dim]);

        # 缩放点积注意力
        let scale = 1.0 / sqrt(f32(head_dim));
        let attn = softmax(matmul(q_heads, transpose(k_heads, -2, -1)) * scale);
        let out = matmul(attn, v_heads);

        # 合并头并投影
        let merged = reshape(out, [seq_len, embed_dim]);
        return matmul(merged, out_proj_weight) + out_proj_bias;
    }
}
```

### 层归一化

```rust
neural LayerNorm<normalized_dim: u32> {
    gamma: tensor<f32, [normalized_dim]>,
    beta: tensor<f32, [normalized_dim]>,

    forward(input: vec<normalized_dim, f32>) -> vec<normalized_dim, f32> {
        let mean = reduce_sum(input) / f32(normalized_dim);
        let variance = reduce_sum((input - mean) * (input - mean)) / f32(normalized_dim);
        let normalized = (input - mean) / sqrt(variance + 1e-5);
        return gamma * normalized + beta;
    }
}
```

## 张量类型系统

### tensor 类型

```rust
# 标量张量
let x: tensor<f32, []>;

# 一维张量（向量）
let v: tensor<f32, [128]>;

# 二维张量（矩阵）
let w: tensor<f32, [256, 128]>;

# 四维张量（卷积核）
let kernel: tensor<f32, [64, 3, 3, 3]>;

# 动态维度（运行时确定）
let dynamic: tensor<f32, [?, 768]>;
```

### 张量操作

| 操作 | 语法 | 描述 |
| :--- | :--- | :--- |
| 矩阵乘法 | `matmul(a, b)` | 映射为 Tensor Core MMA 指令 |
| 卷积 | `conv2d(weight, input, stride, padding)` | 映射为 Winograd / im2col + matmul |
| 重塑 | `reshape(x, [dims])` | 零拷贝视图变换 |
| 转置 | `transpose(x, dim0, dim1)` | 维度交换 |
| 归约求和 | `reduce_sum(x)` | 跨维度求和 |
| Softmax | `softmax(x)` | 数值稳定 softmax |
| 拼接 | `concat([a, b], dim)` | 沿指定维度拼接 |

### 精度控制

```rust
# 指定计算精度
neural LinearLayer<in_dim: u32, out_dim: u32> @precision(half) {
    weight: tensor<f16, [out_dim, in_dim]>,
    bias: tensor<f32, [out_dim]>,

    forward(input: vec<in_dim, f32>) -> vec<out_dim, f32> {
        # 输入自动转换为 f16 进行 Tensor Core 计算
        # 输出自动回转为 f32
        return matmul(weight, input) + bias;
    }
}
```

| 精度装饰器 | 描述 | 适用硬件 |
| :--- | :--- | :--- |
| `@precision(full)` | f32 全精度计算 | 所有 GPU |
| `@precision(half)` | f16 混合精度，权重 f16，累加 f32 | NVIDIA Tensor Core, AMD Matrix Core |
| `@precision(int8)` | INT8 量化推理 | NVIDIA Tensor Core (Ampere+) |
| `@precision(bfloat16)` | BF16 混合精度 | NVIDIA Hopper+, AMD MI300+ |

## 权重管理

### 权重加载

```rust
# 从资产加载预训练权重
neural NeRFMLP @precision(half) {
    # [Weights("models/nerf/mlp_weights.bin")] 指定权重资产路径
    weight_0: tensor<f16, [256, 63]>  @weights("models/nerf/layer0_weight.bin"),
    bias_0: tensor<f16, [256]>        @weights("models/nerf/layer0_bias.bin"),
    weight_1: tensor<f16, [256, 256]> @weights("models/nerf/layer1_weight.bin"),
    bias_1: tensor<f16, [256]>        @weights("models/nerf/layer1_bias.bin"),
    weight_2: tensor<f16, [256, 256]> @weights("models/nerf/layer2_weight.bin"),
    bias_2: tensor<f16, [256]>        @weights("models/nerf/layer2_bias.bin"),
    weight_3: tensor<f16, [4, 256]>   @weights("models/nerf/layer3_weight.bin"),
    bias_3: tensor<f16, [4]>          @weights("models/nerf/layer3_bias.bin"),

    forward(pos_dir: vec<63, f32>) -> vec<4, f32> {
        var x = relu_vec(matmul(weight_0, pos_dir) + bias_0);
        x = relu_vec(matmul(weight_1, x) + bias_1);
        x = relu_vec(matmul(weight_2, x) + bias_2);
        return sigmoid_vec(matmul(weight_3, x) + bias_3);
    }
}
```

### 权重布局

```rust
# 指定权重内存布局
@layout(row_major)    # 行优先（默认）
@layout(column_major) # 列优先（某些 BLAS 实现更高效）
@layout(nchw)         # 卷积核 NCHW 布局
@layout(nhwc)         # 卷积核 NHWC 布局（Tensor Core 友好）
```

## 编译路径

### Tensor Core 调度

gg-neural 编译器将 `matmul` 操作映射为 GPU 原生矩阵乘法指令：

| GPU 架构 | 指令 | 矩阵分块 |
| :--- | :--- | :--- |
| NVIDIA Ampere+ | `mma.sync` (HMMA) | 16×8×16 (f16) / 8×8×4 (f64) |
| AMD CDNA | `mfma` | 16×16×16 (f16) / 32×32×8 (bf16) |
| Apple M 系列 | AMX 矩阵操作 | 外部矩阵协处理器 |

### SPIR-V Cooperative Matrix

对于不支持 Tensor Core 的 GPU，编译器回退到 SPIR-V Cooperative Matrix 扩展：

```rust
# 编译器自动生成的 SPIR-V Cooperative Matrix 伪代码
# matmul(weight, input) 映射为：
#
# %a = OpLoad %matNxM %weight_tile
# %b = OpLoad %matMxK %input_tile
# %c = OpMatrixTimesScalar %a %b    # 或 OpCooperativeMatrixMulAdd
```

### 编译流程

```
.ggns 源码 (gg-neural)
    ↓
前端 (C#) → AST → 类型检查
    ↓
    ├── Tensor Core 路径 → 分块策略 → MMA 指令序列 → 嵌入 SPIR-V Compute Shader
    │
    ├── Cooperative Matrix 路径 → SPIR-V + SPV_KHR_cooperative_matrix
    │
    └── 回退路径 → WMMA / 子组矩阵乘法 / 纯标量实现
```

## 应用场景

### NeRF 实时渲染

```rust
# nerf_realtime.ggns
neural NeRFMLP @precision(half) {
    encoding_weight: tensor<f16, [256, 63]>,
    encoding_bias: tensor<f16, [256]>,
    density_weight: tensor<f16, [1, 256]>,
    density_bias: tensor<f16, [1]>,
    color_weight: tensor<f16, [3, 256]>,
    color_bias: tensor<f16, [3]>,

    forward(pos_dir: vec<63, f32>) -> vec<4, f32> {
        let h = relu_vec(matmul(encoding_weight, pos_dir) + encoding_bias);
        let density = sigmoid(matmul(density_weight, h) + density_bias);
        let color = sigmoid_vec(matmul(color_weight, h) + color_bias);
        return vec.from(density, color);
    }
}

[Compute]
[WorkgroupSize(8, 8, 1)]
micro nerf_render(
    [Builtin(global_invocation_id)] global_id: vec3<u32>,
    [NeuralModel] model: NeRFMLP
) {
    let pixel = vec2<f32>(global_id.xy);
    let ray = generate_camera_ray(pixel, uniforms.view, uniforms.projection);

    # 射线行进，每步调用 MLP
    var color = vec3<f32>(0.0, 0.0, 0.0);
    var alpha = 0.0;
    for (step in range(0, MAX_STEPS)) {
        let pos = ray.origin + ray.direction * f32(step) * STEP_SIZE;
        let pos_dir = encode_position(pos, ray.direction);
        let output = model.forward(pos_dir);
        let density = output.x;
        let sample_color = output.yzw;
        let weight = density * (1.0 - alpha);
        color = color + sample_color * weight;
        alpha = alpha + weight;
        if (alpha > 0.99) { break; }
    }

    textureStore(output_image, global_id.xy, vec4<f32>(color, alpha));
}
```

### 3D Gaussian Splatting

```rust
# 3dgs.ggns
neural GaussianDecoder @precision(half) {
    sh_weight: tensor<f16, [48, 3]>,
    sh_bias: tensor<f16, [3]>,

    forward(sh_features: vec<48, f32>) -> vec<3, f32> {
        return sigmoid_vec(matmul(sh_weight, sh_features) + sh_bias);
    }
}
```

### 超分辨率

```rust
# upscale.ggns
neural UpscaleConv @precision(half) {
    conv1_weight: tensor<f16, [64, 3, 3, 3]>,
    conv1_bias: tensor<f16, [64]>,
    conv2_weight: tensor<f16, [64, 64, 3, 3]>,
    conv2_bias: tensor<f16, [64]>,
    conv3_weight: tensor<f16, [12, 64, 3, 3]>,
    conv3_bias: tensor<f16, [12]>,

    forward(input: tensor<f32, [3, height, width]>) -> tensor<f32, [3, height * 2, width * 2]> {
        let x = relu_vec(conv2d(conv1_weight, input, 1, 1) + conv1_bias);
        let y = relu_vec(conv2d(conv2_weight, x, 1, 1) + conv2_bias);
        let out = conv2d(conv3_weight, y, 1, 1) + conv3_bias;
        return pixel_shuffle(out, 2);
    }
}
```

### DLSS 风格帧生成

```rust
# frame_gen.ggns
neural FrameGenerator @precision(half) {
    # 光流编码器
    flow_enc_conv1: Conv2D<3, 32, 3, 1, 1>,
    flow_enc_conv2: Conv2D<32, 64, 3, 2, 1>,

    # 帧合成器
    synthesis_conv1: Conv2D<68, 64, 3, 1, 1>,
    synthesis_conv2: Conv2D<64, 32, 3, 1, 1>,
    synthesis_conv3: Conv2D<32, 3, 3, 1, 1>,

    forward(
        current_frame: tensor<f32, [3, h, w]>,
        previous_frame: tensor<f32, [3, h, w]>,
        depth: tensor<f32, [1, h, w]>,
        motion: tensor<f32, [2, h, w]>
    ) -> tensor<f32, [3, h, w]> {
        let flow_feat = relu_vec(flow_enc_conv2.forward(relu_vec(flow_enc_conv1.forward(motion))));
        let combined = concat([current_frame, previous_frame, depth, flow_feat], 0);
        let x = relu_vec(synthesis_conv1.forward(combined));
        x = relu_vec(synthesis_conv2.forward(x));
        return sigmoid_vec(synthesis_conv3.forward(x));
    }
}
```

## 性能指南

### Tensor Core 利用率

| 建议 | 原因 |
| :--- | :--- |
| 矩阵维度对齐到 8 或 16 的倍数 | Tensor Core 以 8×8 或 16×8 分块运算 |
| 使用 `@precision(half)` | f16 吞吐量是 f32 的 2-8 倍 |
| 批量推理优于逐像素推理 | 减少 kernel launch 开销 |
| 权重使用 NHWC 布局 | Tensor Core 友好 |

### 内存布局

```rust
# 推荐：权重常驻 GPU 显存
@weights_layout(device_local)  # 默认，权重常驻显存

# 可选：权重按需流式加载（大模型场景）
@weights_layout(streaming)
```

### 与渲染管线的融合

```rust
# 最佳实践：在渲染帧内完成推理，避免 CPU 回读
[Compute]
[WorkgroupSize(8, 8, 1)]
micro deferred_neural_shading(
    [Builtin(global_invocation_id)] global_id: vec3<u32>,
    [NeuralModel] model: NeuralShadingMLP
) {
    let gbuffer = load_gbuffer(global_id.xy);
    let shading_input = encode_shading_input(gbuffer);
    let color = model.forward(shading_input);
    textureStore(output_image, global_id.xy, color);
}
```

## 与 gg-shader 的关系

gg-neural 是 gg-shader 的扩展，不是独立语言：

| 维度 | gg-shader | gg-neural |
| :--- | :--- | :--- |
| 文件扩展名 | `.shader` | `.shader`（同一文件） |
| 计算单元 | `micro` 函数 | `neural` 块 |
| 调用关系 | 可调用 `neural` 块的 `forward()` | 可调用 `micro` 函数 |
| 编译器 | 同一个编译器 | 同一个编译器 |
| IR | 同一套 Shader IR | 扩展了 Tensor IR 节点 |

在同一个 `.shader` 文件中混合使用：

```rust
# 混合使用示例
neural SmallMLP @precision(half) {
    w0: tensor<f16, [64, 32]>,
    b0: tensor<f16, [64]>,
    w1: tensor<f16, [3, 64]>,
    b1: tensor<f16, [3]>,

    forward(x: vec<32, f32>) -> vec<3, f32> {
        let h = relu_vec(matmul(w0, x) + b0);
        return sigmoid_vec(matmul(w1, h) + b1);
    }
}

# micro 函数中调用 neural 块
[Fragment]
micro ps_main(input: VertexOutput) -> vec4<f32> {
    let features = extract_features(input);  # micro 函数
    let neural_color = SmallMLP.forward(features);  # neural 推理
    let final_color = tone_map(neural_color);  # micro 函数
    return vec4<f32>(final_color, 1.0);
}

micro extract_features(input: VertexOutput) -> vec<32, f32> {
    # 传统着色器逻辑：提取位置、法线、UV 等特征
    ...
}

micro tone_map(color: vec<3, f32>) -> vec<3, f32> {
    # ACES Tone Mapping
    ...
}
```

## 下一步

- 阅读 [gg-shader 指南](gg-shader.md) 了解基础着色器语法
- 阅读 [渲染系统](../development/rendering.md) 了解 RHI 抽象层
- 查看 [示例项目](../../examples/) 了解实际用法
