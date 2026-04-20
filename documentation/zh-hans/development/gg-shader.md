# gg-shader 着色器语言指南

本文档介绍 gg-shader 着色器语言的语法特性和使用方法。

## 语言概述

gg-shader 是 gg 引擎自主设计的着色器语言，具有以下特性：

| 特性 | 描述 |
| :--- | :--- |
| 纯 C# 实现 | 零 C/C++ 工具链依赖 |
| 直接生成 SPIR-V | 无需 glslang 等中间工具 |
| 类型后置语法 | 类似 Rust/WGSL |
| 元编程集成 | `<% %>` 块支持静态变体生成 |

## 基础语法

### 函数定义

```rust
fn vs_main(position: vec3<f32>, uv: vec2<f32>) -> VertexOutput {
    var output: VertexOutput;
    output.position = vec4<f32>(position, 1.0);
    output.uv = uv;
    return output;
}

fn ps_main(uv: vec2<f32>) -> vec4<f32> {
    return vec4<f32>(1.0, 0.0, 0.0, 1.0);
}
```

### 类型系统

| 类型 | 描述 |
| :--- | :--- |
| `bool` | 布尔值 |
| `i32`, `u32` | 整数 |
| `f32`, `f64` | 浮点数 |
| `vec2<T>`, `vec3<T>`, `vec4<T>` | 向量 |
| `mat2<T>`, `mat3<T>`, `mat4<T>` | 矩阵 |
| `texture_2d<T>` | 2D 纹理 |
| `sampler` | 采样器 |

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
@vertex
fn vs_main(input: VertexInput) -> VertexOutput {
    var output: VertexOutput;
    output.position = uniforms.projection * uniforms.view * uniforms.model * vec4<f32>(input.position, 1.0);
    output.uv = input.uv;
    output.world_pos = (uniforms.model * vec4<f32>(input.position, 1.0)).xyz;
    return output;
}
```

### 片段着色器

```rust
@fragment
fn ps_main(input: VertexOutput) -> vec4<f32> {
    var color = textureSample(diffuse_texture, sampler, input.uv);
    return color;
}
```

### 计算着色器

```rust
@compute @workgroup_size(256)
fn cs_main(@builtin(global_invocation_id) global_id: vec3<u32>) {
    var index = global_id.x;
    // 计算逻辑
}
```

## 统一变量与绑定

```rust
@group(0) @binding(0)
var<uniform> uniforms: UniformBuffer;

@group(0) @binding(1)
var diffuse_texture: texture_2d<f32>;

@group(0) @binding(2)
var sampler: sampler;
```

## 元编程

### 静态变体生成

```rust
<% foreach (var feature in new[] { "DIFFUSE", "NORMAL", "SPECULAR" }) { %>
    <% if (feature == "DIFFUSE") { %>
        var diffuse = textureSample(diffuse_texture, sampler, input.uv);
    <% } %>
    <% if (feature == "NORMAL") { %>
        var normal = textureSample(normal_texture, sampler, input.uv);
    <% } %>
<% } %>
```

### 条件编译

```rust
<% if (MACRO.RENDER_PATH == "FORWARD") { %>
    // 前向渲染逻辑
<% } else if (MACRO.RENDER_PATH == "DEFERRED") { %>
    // 延迟渲染逻辑
<% } %>
```

## 模块系统

### 导出

```rust
// math.ggs
export fn lerp(a: f32, b: f32, t: f32) -> f32 {
    return a + (b - a) * t;
}

export fn saturate(x: f32) -> f32 {
    return clamp(x, 0.0, 1.0);
}
```

### 导入

```rust
import math;

fn ps_main(uv: vec2<f32>) -> vec4<f32> {
    var value = lerp(0.0, 1.0, 0.5);
    return vec4<f32>(value, value, value, 1.0);
}
```

## 内置函数

### 数学函数

| 函数 | 描述 |
| :--- | :--- |
| `abs`, `sign` | 绝对值、符号 |
| `floor`, `ceil`, `round` | 取整 |
| `min`, `max`, `clamp` | 范围限制 |
| `mix`, `lerp` | 线性插值 |
| `step`, `smoothstep` | 阶梯函数 |
| `sin`, `cos`, `tan` | 三角函数 |
| `pow`, `exp`, `log` | 指数对数 |
| `sqrt`, `inversesqrt` | 平方根 |
| `dot`, `cross` | 向量运算 |
| `normalize`, `length` | 向量归一化 |

### 纹理函数

| 函数 | 描述 |
| :--- | :--- |
| `textureSample` | 纹理采样 |
| `textureLoad` | 纹理加载 |
| `textureStore` | 纹理存储 |

## 编译器架构

```
.ggs 源码
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

## 与引擎集成

### 材质定义

```tsx
export material PBRMaterial {
    vertex_shader: "shaders/pbr_vertex.ggs";
    fragment_shader: "shaders/pbr_fragment.ggs";
    
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
var mesh = asset.load<Mesh>("models/character.mesh");
var material = asset.load<Material>("materials/character.mat");

var entity = create_entity();
entity.add(MeshFilter { mesh: mesh });
entity.add(Material { material: material });
entity.add(Transform { position: vec3(0, 0, 0) });
```

## 调试技巧

### 颜色调试

```rust
fn ps_main(input: VertexOutput) -> vec4<f32> {
    // 显示法线
    return vec4<f32>(input.normal * 0.5 + 0.5, 1.0);
    
    // 显示 UV
    return vec4<f32>(input.uv, 0.0, 1.0);
}
```

### 热重载

编辑器支持着色器热重载，修改 `.ggs` 文件后自动重新编译并应用。

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

- 阅读 [渲染系统](rendering.md) 了解 RHI 抽象层
- 查看 [示例项目](../../examples/) 了解实际用法
