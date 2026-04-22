# gg-shader 着色器语言指南

本文档介绍 gg-shader 着色器语言的语法特性和使用方法。

## 语言概述

gg-shader 是 gg 引擎自主设计的着色器语言，具有以下特性：

| 特性          | 描述                |
| :---------- | :---------------- |
| 纯 C# 实现     | 零 C/C++ 工具链依赖     |
| 直接生成 SPIR-V | 无需 glslang 等中间工具  |
| 类型后置语法      | 类似 Rust/WGSL      |
| 元编程集成       | `<% %>` 块支持静态变体生成 |

### 一切皆 Shader

gg-shader 的设计基于一个核心认知：**从传统光栅化到光线追踪，再到神经辐射场（NeRF）乃至扩散模型（Stable Diffusion），其核心都可以被抽象为同一个数学概念——给定一组输入坐标或参数，计算并输出一个颜色值（以及可能的辅助数据）**。

这种抽象正是**着色器 (Shader)** 的广义定义。在 gg 引擎中，`micro` 函数被定义为 GPU 可编程管线的**通用最小执行单元**。无论是计算光线交点、采样 MLP 权重还是去噪潜空间，它们都是"输入数据，输出颜色"的特化变体。这种统一使得开发者可以在不改变上层 ECS 代码的情况下，将同一个游戏逻辑从光栅化模式无缝切换到全景光追模式，甚至未来的神经渲染模式。

## 广义着色器抽象

### 数学本质的统一：从坐标到颜色的映射

无论底层物理过程多复杂，最终在屏幕上显示一个像素的过程都可以抽象为函数：

```
Color = f(Ray, UV, Position, Noise, Time, ...)
```

| 渲染范式 | 具体的 `f(x)` 实现 | gg-shader 中的抽象输入 |
| :--- | :--- | :--- |
| **传统光栅化** | 插值 UV → 查纹理 → BRDF 光照计算 | `uv: vec2<f32>` → `vec4<f32>` |
| **光线追踪** | 射线求交 → 递归光照采样 | `ray: Ray` → `vec4<f32>` |
| **NeRF (神经辐射场)** | 5D 坐标 (x,y,z,θ,φ) → MLP 网络推理 → 密度与颜色 | `pos: vec3<f32>, dir: vec3<f32>` → `vec4<f32>` |
| **扩散模型** | 噪声图 + 提示词嵌入 → UNet 去噪 → 像素颜色 | `seed: u32, prompt_hash: u32` → `vec4<f32>` |

只要输入是一组**结构化参数**，输出是**颜色值**，它就是一个广义的着色器。gg-shader 的语法 `micro fn(...) -> vec4` 正是为此设计。

### 执行载体的归一化：计算着色器的泛用性

现代图形 API (Vulkan/DX12/Metal) 中的**计算着色器 (Compute Shader)** 已经打破了图形管线的束缚，它允许在 GPU 上执行任意的大规模并行数学计算。

- **光追 (Ray Tracing)**：在 Vulkan/DX12 中，`RayGen`、`ClosestHit` 本质上就是特殊的计算着色器入口。
- **NeRF / 3DGS**：推理过程可封装在一个巨大的计算着色器中，每个线程负责一根射线或一个高斯点的光栅化。
- **Stable Diffusion**：UNet 模型被编译为一系列矩阵乘加操作，这些操作通过计算着色器（或专门的 Tensor Core 指令）执行。

在 gg 引擎中，`gg-shader` 语言**不区分** `[Vertex]`、`[Fragment]` 还是 `[Compute]` 的底层差异——它们都是 `micro` 函数。不同的后端（光栅化后端 vs. 神经渲染后端）会将同一个 `gg-shader` 逻辑编译为对应 API 的执行单元。

### 渲染不可知论的实现机制

这正是 gg 引擎"机制与策略分离"原则的体现。通过**元编程**和**抽象语法树 (AST) 变换**来统一不同渲染范式之间的差异：

```rust
# 定义一个通用的"获取颜色"接口，不关心底层是采样纹理还是跑神经网络
micro shade_pixel(input: ShadingInput) -> vec4<f32> {
    <% if (RENDER_MODE == "RASTER") { %>
        return textureSample(albedo_map, sampler, input.uv);
    <% } else if (RENDER_MODE == "RAY_TRACE") { %>
        return trace_ray(input.ray_origin, input.ray_dir);
    <% } else if (RENDER_MODE == "NERF") { %>
        # 编译为运行 MLP 网络的计算着色器
        return evaluate_nerf_network(input.ray_pos, input.ray_dir);
    <% } else if (RENDER_MODE == "DIFFUSION") { %>
        # 编译为降噪迭代逻辑
        return diffusion_step(input.noise, input.step);
    <% } %>
}
```

不同后端的编译输出路径：

| 后端类型 | 编译输出 | 执行方式 |
| :--- | :--- | :--- |
| **传统 Shader** | SPIR-V 字节码 | Vulkan / Metal / D3D12 直接执行 |
| **光追 Shader** | SPIR-V + `GL_EXT_ray_tracing` 入口标记 | Vulkan Ray Tracing Pipeline 执行 |
| **神经渲染** | Tensor Core 调度序列 / 预编译权重 Blob | RHI `Dispatch` 间接执行 |
| **扩散模型** | `[External]` 绑定，由引擎扩散模型后端接管 | Tensor Core 预编译模型执行 |

### Shader 抽象的边界

Shader 抽象并非万能，理解其边界有助于正确使用：

| 能被 Shader 抽象覆盖 | 需要额外处理（不归 Shader 管） |
| :--- | :--- |
| **像素/射线的颜色计算逻辑** | **数据组织与上传**（例如 NeRF 的 Hash Grid 显存管理） |
| **几何形变与顶点变换** | **管线状态切换**（光栅化 vs. 光线发射） |
| **材质参数响应** | **渲染顺序与合成**（透明物体排序） |

**Stable Diffusion 的特殊性**：虽然计算过程可以用 Compute Shader 实现，但 gg 引擎不会用 `gg-shader` 去编写 UNet 架构代码。而是通过 `[External]` 绑定：`gg-shader` 定义一个黑盒函数 `denoise_step`，其实现由引擎的**扩散模型后端**通过 Tensor Core 调度执行：

```rust
# gg-shader 只声明接口，实现由引擎后端提供
[External("diffusion_denoise_step")]
micro denoise_step(noise: texture_2d<f32>, step: u32, prompt_hash: u32) -> vec4<f32>;
```

## 基础语法

### 注释

gg-shader 支持两种注释语法：

| 语法 | 描述 |
| :--- | :--- |
| `#` | 行注释，从 `#` 到行末的内容被忽略 |
| `<# #>` | 块注释，支持嵌套 |

```rust
# 这是行注释
let x = 1.0; # 行末注释

<# 这是块注释 #>
let y = 2.0;

<# 嵌套
   <# 内层注释 #>
#>
let z = 3.0;
```

### 函数定义

gg-shader 使用 `micro` 关键字定义着色器函数：

```rust
# 顶点着色器
[Vertex]
micro vs_main([Builtin(vertex_index)] idx: u32) -> VertexOutput {
    ...
}

# 片段着色器
[Fragment]
micro ps_main(input: VertexOutput) -> vec4<f32> {
    ...
}

# 计算着色器
[Compute]
[WorkgroupSize(8, 8, 1)]
micro cs_main([Builtin(global_invocation_id)] global_id: vec3<u32>) {
    ...
}
```

### 神经层定义

gg-shader 使用 `neural` 关键字定义神经网络层，直接利用 GPU 的 Tensor Core / Matrix Core 执行推理：

```rust
# 线性层
neural LinearLayer<in_dim: u32, out_dim: u32> @precision(half) {
    weight: tensor<f16, [out_dim, in_dim]>,
    bias: tensor<f16, [out_dim]>,

    forward(input: vec<in_dim, f32>) -> vec<out_dim, f32> {
        return matmul(weight, input) + bias;
    }
}

# 在 micro 函数中调用
[Fragment]
micro ps_main(input: VertexOutput) -> vec4<f32> {
    let features = extract_features(input);
    let neural_color = LinearLayer<32, 3>.forward(features);
    return vec4<f32>(neural_color, 1.0);
}
```

`neural` 块与 `micro` 函数的核心区别：

| 维度 | `micro` 函数 | `neural` 块 |
| :--- | :--- | :--- |
| 执行单元 | CUDA Core / Stream Processor | Tensor Core / Matrix Core |
| 数据粒度 | 标量、向量 | 矩阵分块、张量 |
| 典型用途 | 光照、纹理采样 | MLP 推理、卷积、注意力 |

详细的神经着色器编程指南请参考 [gg-neural 指南](gg-neural.md)。

```rust
# 使用 using 语句简化泛型
using gg_shader::f32::{vec2, vec3, vec4};

micro vs_main(position: vec3, uv: vec2) -> VertexOutput {
    let mut output: VertexOutput;
    output.position = vec4(position, 1.0);
    output.uv = uv;
    return output;
}

micro ps_main(uv: vec2) -> vec4 {
    return vec4(1.0, 0.0, 0.0, 1.0);
}
```

### 类型系统

| 类型                              | 描述    |
| :------------------------------ | :---- |
| `bool`                          | 布尔值   |
| `i32`, `u32`                    | 整数    |
| `f32`, `f64`                    | 浮点数   |
| `vec2`, `vec3`, `vec4`          | 向量 (f32) |
| `mat2`, `mat3`, `mat4`          | 矩阵 (f32) |
| `texture_2d<T>`                 | 2D 纹理 |
| `sampler`                       | 采样器   |

#### 类型定义说明

- `vec2`、`vec3`、`vec4` 是 `Vector2::<f32>`、`Vector3::<f32>`、`Vector4::<f32>` 的简写形式
- 本质上，这些是泛型类的具体实例化，基础泛型类定义为 `class Vector2<T>`
- `vec2` 本身已经是 `Vector2::<f32>` 的简写，因此后面不需要再跟泛型参数
- 在 term 域（表达式中），创建实例时需要使用完整的泛型语法：`Vector2::<f32>(0, 0)`

```rust
# 正确：使用简写形式
type Position = vec2;

# 正确：在 term 域中使用完整泛型语法
let pos = Vector2::<f32>(100.0, 200.0);
let dir = Vector3::<f32>(1.0, 0.0, 0.0);
```

### 结构体

```rust
struct VertexInput {
    position: vec3<f32>,
    uv: vec2<f32>,
    normal: vec3<f32>,
}

struct VertexOutput {
    position: vec4<f32>,
    uv: vec2<f32>,
    world_pos: vec3<f32>,
}

struct UniformBuffer {
    model: mat4<f32>,
    view: mat4<f32>,
    projection: mat4<f32>,
}
```

## 着色器阶段

### 顶点着色器

```rust
[Vertex]
micro vs_main(input: VertexInput) -> VertexOutput {
    let mut output: VertexOutput;
    output.position = uniforms.projection * uniforms.view * uniforms.model * vec4<f32>(input.position, 1.0);
    output.uv = input.uv;
    output.world_pos = (uniforms.model * vec4<f32>(input.position, 1.0)).xyz;
    return output;
}
```

### 片段着色器

```rust
[Fragment]
micro ps_main(input: VertexOutput) -> vec4<f32> {
    let color = textureSample(diffuse_texture, sampler, input.uv);
    return color;
}
```

### 计算着色器

```rust
[Compute]
[WorkgroupSize(256)]
micro cs_main([Builtin(global_invocation_id)] global_id: vec3<u32>) {
    let index = global_id.x;
    # 计算逻辑
}
```

### 光线追踪着色器

光线追踪管线引入了三种专用的着色器阶段，它们本质上都是特殊的计算着色器入口：

#### 光线生成着色器

```rust
[RayGen]
micro ray_gen() {
    let pixel_coord = vec2<f32>(builtin.launch_id.xy);
    let ray = generate_camera_ray(pixel_coord, uniforms.view, uniforms.projection);
    trace_ray(acceleration_structure, ray_flags_none, 0u, 0u, 1u, 0u, ray);
}
```

#### 最近命中着色器

```rust
[ClosestHit]
micro closest_hit([HitAttr] hit_attr: HitAttribute) {
    let hit_pos = builtin.world_ray_origin + builtin.world_ray_direction * hit_attr.t;
    let normal = hit_attr.normal;
    let lighting = compute_pbr_lighting(hit_pos, normal, uniforms.light_dir);
    payload.color = lighting;
}
```

#### 未命中着色器

```rust
[Miss]
micro miss() {
    let dir = normalize(builtin.world_ray_direction);
    let sky_color = sample_skybox(dir);
    payload.color = sky_color;
}
```

| 阶段 | 装饰器 | 描述 |
| :--- | :--- | :--- |
| 光线生成 | `[RayGen]` | 每个像素发射一根射线，是光追管线的入口 |
| 最近命中 | `[ClosestHit]` | 射线命中几何体时调用，执行材质着色计算 |
| 未命中 | `[Miss]` | 射线未命中任何几何体时调用，通常返回天空颜色 |

## 统一变量与绑定

```rust
[Group(0), Binding(0)]
let<uniform> uniforms: UniformBuffer;

[Group(0), Binding(1)]
let diffuse_texture: texture_2d<f32>;

[Group(0), Binding(2)]
let sampler: sampler;
```

## 元编程

### 静态变体生成

```rust
<% foreach (var feature in new[] { "DIFFUSE", "NORMAL", "SPECULAR" }) { %>
    <% if (feature == "DIFFUSE") { %>
        let diffuse = textureSample(diffuse_texture, sampler, input.uv);
    <% } %>
    <% if (feature == "NORMAL") { %>
        let normal = textureSample(normal_texture, sampler, input.uv);
    <% } %>
<% } %>
```

### 条件编译

```rust
<% if (MACRO.RENDER_PATH == "FORWARD") { %>
    # 前向渲染逻辑
<% } else if (MACRO.RENDER_PATH == "DEFERRED") { %>
    # 延迟渲染逻辑
<% } %>
```

### 循环语法

```rust
<% loop i in range(0, 10) { %>
    let value_<%= i %> = <%= i * 2 %>;
<% } %>
```

### 模式匹配

```rust
<% match value { %>
    <% case 0 { %>
        # 处理 0 的情况
    <% } %>
    <% case 1 { %>
        # 处理 1 的情况
    <% } %>
    <% case _ { %>
        # 处理其他情况
    <% } %>
<% } %>
```

## 模块系统

### 导出

```rust
# math.shader
micro lerp(a: f32, b: f32, t: f32) -> f32 {
    return a + (b - a) * t;
}

micro saturate(x: f32) -> f32 {
    return clamp(x, 0.0, 1.0);
}
```

### 导入

```rust
import math;

micro ps_main(uv: vec2<f32>) -> vec4<f32> {
    let value = lerp(0.0, 1.0, 0.5);
    return vec4<f32>(value, value, value, 1.0);
}
```

## 内置函数

### 数学函数

| 函数                       | 描述     |
| :----------------------- | :----- |
| `abs`, `sign`            | 绝对值、符号 |
| `floor`, `ceil`, `round` | 取整     |
| `min`, `max`, `clamp`    | 范围限制   |
| `mix`, `lerp`            | 线性插值   |
| `step`, `smoothstep`     | 阶梯函数   |
| `sin`, `cos`, `tan`      | 三角函数   |
| `pow`, `exp`, `log`      | 指数对数   |
| `sqrt`, `inversesqrt`    | 平方根    |
| `dot`, `cross`           | 向量运算   |
| `normalize`, `length`    | 向量归一化  |

### 纹理函数

| 函数              | 描述   |
| :-------------- | :--- |
| `textureSample` | 纹理采样 |
| `textureLoad`   | 纹理加载 |
| `textureStore`  | 纹理存储 |

## 编译器架构

### 单后端编译流程（传统光栅化）

```
.shader 源码
    ↓
前端 (C#)
    ↓
AST (抽象语法树)
    ↓
元编程展开
    ↓
IR (中间表示)
    ↓
后端 (C# + Spv.Generator)
    ↓
SPIR-V 字节码
```

### 多后端编译路径

gg-shader 编译器根据目标渲染后端选择不同的编译路径：

```
.shader 源码
    ↓
前端 (C#) → AST → 元编程展开 → IR
    ↓
    ├── 光栅化后端 → SPIR-V 字节码 → Vulkan / Metal / D3D12
    │
    ├── 光线追踪后端 → SPIR-V + GL_EXT_ray_tracing → Vulkan RT Pipeline
    │
    ├── 神经渲染后端 → Tensor Core 调度序列 / 权重 Blob → RHI Dispatch
    │
    └── 扩散模型后端 → [External] 绑定 → 引擎 Tensor Core 预编译模型
```

| 编译路径 | 输出格式 | 后端选择依据 |
| :--- | :--- | :--- |
| **光栅化** | SPIR-V 字节码 | `RENDER_MODE == "RASTER"` |
| **光线追踪** | SPIR-V + 光追扩展标记 | `RENDER_MODE == "RAY_TRACE"` |
| **神经渲染** | Tensor Core 调度序列 + 权重嵌入 | `RENDER_MODE == "NERF"` |
| **扩散模型** | `[External]` 符号绑定 | `RENDER_MODE == "DIFFUSION"` |

编译器在元编程展开阶段根据 `RENDER_MODE` 宏选择对应的代码路径，IR 层之后的后端负责将中间表示翻译为目标格式。对于 `[External]` 绑定的函数，编译器仅生成符号引用，实际实现在运行时由引擎后端注入。

## 与引擎集成

### 材质定义

```tsx
material PBRMaterial {
    vertex_shader: "shaders/pbr_vertex.shader";
    fragment_shader: "shaders/pbr_fragment.shader";
    
    properties: {
        albedo: texture_2d;
        normal: texture_2d;
        metallic: f32 = 0.5;
        roughness: f32 = 0.5;
    };
}
```

### 使用材质

```tsx
let mesh = asset.load<Mesh>("models/character.mesh");
let material = asset.load<Material>("materials/character.mat");

let entity = create_entity();
entity.add(MeshFilter { mesh: mesh });
entity.add(Material { material: material });
entity.add(Transform { position: vec3(0, 0, 0) });
```

### NeRF 着色器集成

神经渲染材质通过 `gg-shader` 定义推理接口，实际计算由引擎神经渲染后端执行：

```rust
# nerf_shade.shader
struct NeRFInput {
    ray_pos: vec3<f32>,
    ray_dir: vec3<f32>,
}

[Compute]
[WorkgroupSize(8, 8, 1)]
micro nerf_render([Builtin(global_invocation_id)] global_id: vec3<u32>) {
    let pixel = vec2<f32>(global_id.xy);
    let ray = generate_camera_ray(pixel, uniforms.view, uniforms.projection);
    let color = evaluate_nerf_network(ray.origin, ray.direction);
    textureStore(output_image, global_id.xy, color);
}

# 由引擎神经渲染后端提供实现
[External("nerf_evaluate")]
micro evaluate_nerf_network(pos: vec3<f32>, dir: vec3<f32>) -> vec4<f32>;
```

```tsx
# 材质定义
material NeRFMaterial {
    compute_shader: "shaders/nerf_shade.shader";
    
    properties: {
        model: Handle<NeRFModel>;
        quality: Quality = High;
    };
}
```

### 扩散模型着色器集成

扩散模型材质定义去噪步骤的接口，UNet 推理由引擎扩散模型后端接管：

```rust
# diffusion_shade.shader
[Compute]
[WorkgroupSize(8, 8, 1)]
micro diffusion_render([Builtin(global_invocation_id)] global_id: vec3<u32>) {
    let pixel = vec2<f32>(global_id.xy);
    let step = uniforms.current_step;
    let prompt_hash = uniforms.prompt_hash;
    let color = denoise_step(pixel, step, prompt_hash);
    textureStore(output_image, global_id.xy, color);
}

# 由引擎扩散模型后端提供实现
[External("diffusion_denoise_step")]
micro denoise_step(pixel: vec2<f32>, step: u32, prompt_hash: u32) -> vec4<f32>;
```

```tsx
# 材质定义
material DiffusionMaterial {
    compute_shader: "shaders/diffusion_shade.shader";
    
    properties: {
        model: Handle<DiffusionModel>;
        prompt: string = "";
        steps: u32 = 20;
        guidance_scale: f32 = 7.5;
    };
}
```

### 混合渲染材质

混合渲染将多个后端的输出合成到最终画面：

```tsx
# 混合渲染材质定义
material HybridMaterial {
    primary_shader: "shaders/raster_pbr.shader";
    secondary_shader: "shaders/nerf_shade.shader";
    
    properties: {
        # 光栅化属性
        albedo: texture_2d;
        metallic: f32 = 0.5;
        roughness: f32 = 0.5;
        
        # 神经渲染属性
        nerf_model: Handle<NeRFModel>;
        compose_mode: ComposeMode = DepthBlend;
    };
}
```

```rust
# 混合着色器：将光栅化与神经渲染结果合成
micro compose_hybrid(
    raster_color: vec4<f32>,
    raster_depth: f32,
    nerf_color: vec4<f32>,
    nerf_depth: f32
) -> vec4<f32> {
    if (raster_depth < nerf_depth) {
        return raster_color;
    }
    return nerf_color;
}
```

## 调试技巧

### 颜色调试

```rust
micro ps_main(input: VertexOutput) -> vec4 {
    # 显示法线
    return vec4(input.normal * 0.5 + 0.5, 1.0);
    
    # 显示 UV
    return vec4(input.uv, 0.0, 1.0);
}
```

### 热重载

编辑器支持着色器热重载，修改 `.shader` 文件后自动重新编译并应用。

## 最佳实践

### 性能优化

- 避免分支，使用数学函数替代
- 使用内置函数而非自定义实现
- 合理使用 LOD 和 Mipmap

### 代码组织

- 将通用函数放入模块
- 使用结构体组织数据
- 避免魔法数字，使用常量

## 下一步

- 阅读 [gg-neural 指南](gg-neural.md) 了解神经着色器编程
- 阅读 [渲染系统](../development/rendering.md) 了解 RHI 抽象层
- 查看 [示例项目](../../examples/) 了解实际用法

