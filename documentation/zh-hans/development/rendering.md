# 渲染系统与 RHI 抽象

本文档介绍 gg 引擎的渲染系统设计，包括 RHI 抽象层和 gg-shader 着色器语言。

## 设计哲学

gg 引擎的渲染子系统遵循"机制与策略分离"原则：

| 原则 | 描述 |
| :--- | :--- |
| 零绑定 | 引擎内核不包含对任何单一渲染 API 的硬编码依赖 |
| 数据驱动 | 所有渲染行为由资产与 ECS 组件共同定义 |
| 可替换性 | 开发者可在构建时或运行时选择完全不同的渲染后端 |

## 架构概览

```
┌─────────────────────────────────────────────────────────────┐
│                     游戏逻辑层 (gg 字节码)                     │
│  ┌──────────────────────────────────────────────────────┐   │
│  │         ECS 世界 (MeshFilter, Material, Light)        │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                     渲染抽象层 (C AOT 内核)                    │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐                 │
│  │ RHI      │   │ 设备管理  │   │ 资源缓存  │                │
│  └──────────┘   └──────────┘   └──────────┘                 │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                     渲染后端实现                              │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐                 │
│  │ Vulkan   │   │ Metal    │   │ DirectX  │                │
│  └──────────┘   └──────────┘   └──────────┘                 │
└─────────────────────────────────────────────────────────────┘
```

## 渲染硬件接口 (RHI)

RHI 是一个极简的、面向数据的抽象层，暴露引擎原生句柄而非图形 API 对象。

### 核心概念

| 概念 | 描述 |
| :--- | :--- |
| Device | 设备，负责创建资源、提交命令 |
| Resource | 资源，不透明句柄，代表缓冲、纹理、着色器模块 |
| Pipeline State | 管线状态，描述如何解释着色器输入 |
| Command Table | 命令表，资源绑定与绘制调用的序列化记录 |

### RHI 接口

```rust
pub trait RhiDevice {
    fn create_buffer(&self, desc: BufferDesc) -> BufferHandle;
    fn create_texture(&self, desc: TextureDesc) -> TextureHandle;
    fn create_shader(&self, spirv: &[u8]) -> ShaderHandle;
    fn create_pipeline(&self, desc: PipelineDesc) -> PipelineHandle;
    
    fn begin_frame(&self);
    fn end_frame(&self);
    fn submit(&self, commands: &CommandTable);
}
```

## 与 ECS 集成

渲染所需数据完全存储在 ECS 组件中：

| 组件 | 描述 |
| :--- | :--- |
| `MeshFilter` | 网格数据句柄 |
| `Material` | 由 gg-shader 编译的材质实例 |
| `Transform` | 标准变换 |
| `Light` | 光源数据 |
| `View` | 相机/观察者 |

### 渲染组件

```tsx
export component MeshFilter {
    mesh: Handle<Mesh>;
}

export component Material {
    shader: Handle<Shader>;
    textures: map<string, Handle<Texture>>;
    properties: map<string, Property>;
}

export component View {
    position: vec3;
    rotation: quat;
    projection: Projection;
    render_target: Handle<Texture>;
}
```

## 渲染管线

### 前向渲染

```
┌─────────────┐
│ 几何 pass   │ → 深度预 pass → 不透明物体 → 透明物体
└─────────────┘
       ↓
┌─────────────┐
│ 后处理 pass │ → HDR → Bloom → Tone Mapping → FXAA
└─────────────┘
       ↓
┌──────────────────┐
│ Game UI pass     │ → 游戏 UI 渲染（ECS Canvas 批处理，可接受光照/后处理）
└──────────────────┘
       ↓
┌──────────────────┐
│ Widget pass      │ → 编辑器 Widget 渲染（按需重绘，浮于最顶层）
└──────────────────┘
```

> **注意**：gg 引擎的 UI 渲染分为两个独立的 pass。**Game UI pass** 负责游戏运行时 UI（HUD、血条等），与 3D 渲染管线深度融合，可接受光照和后处理效果；**Widget pass** 负责编辑器 UI，完全隔离于游戏渲染，浮于最顶层。二者共享同一 RHI 后端，但走完全不同的渲染路径。详见 [gg-widget 语言指南](../languages/gg-widget.md)。

### 渲染流程

```rust
fn render_frame(world: &World, device: &dyn RhiDevice) {
    // 1. 收集所有 View
    let views = world.query::<View>();
    
    for view in views {
        // 2. 视锥剔除
        let visible = frustum_cull(world, &view);
        
        // 3. 排序（不透明从前到后，透明从后到前）
        let sorted = sort_by_distance(visible, &view);
        
        // 4. 渲染阴影
        render_shadows(device, &sorted);
        
        // 5. 渲染不透明物体
        render_opaque(device, &sorted);
        
        // 6. 渲染透明物体
        render_transparent(device, &sorted);
        
        // 7. 后处理
        post_process(device, &view);
    }
}
```

## gg-shader 集成

gg-shader 编译器直接生成 SPIR-V 字节码：

```
.shader 源码
    ↓
前端 (C#)
    ↓
IR (中间表示)
    ↓
后端 (C# + Spv.Generator)
    ↓
SPIR-V 字节码
    ↓
RHI 加载
```

### gg-shader 语言特性

| 特性 | 描述 |
| :--- | :--- |
| 类型后置与推断 | `fn ps_main(uv: vec2<f32>) -> vec4<f32> { ... }` |
| gg 元编程集成 | `<% %>` 块在编译时执行，支持静态变体生成 |
| 现代化模块系统 | `import` / `export` 实现着色器逻辑复用 |
| 泛型与接口 (Trait) | 支持编写可复用的、类型安全的着色器算法 |

### gg-shader 编译器架构

- **前端 (纯 C#)**：解析 `.shader` 源码，生成抽象语法树 (AST)，执行元编程展开，产出着色器中间表示 (IR)
- **后端 (纯 C#)**：基于 `Spv.Generator` 等库，直接从 IR 生成符合 Vulkan 规范的 SPIR-V 字节码
- **集成 (C# Source Generator)**：在 MSBuild 过程中，将生成的 SPIR-V 字节码以 `ReadOnlySpan<byte>` 形式嵌入 C# 源码，实现编译时资产嵌入，保证 AOT 兼容性

### 材质系统

```tsx
export material PBRMaterial {
    vertex_shader: "shaders/pbr_vertex.shader";
    fragment_shader: "shaders/pbr_fragment.shader";
    
    properties: {
        albedo: texture_2d;
        normal: texture_2d;
        metallic_roughness: texture_2d;
        metallic: f32 = 0.5;
        roughness: f32 = 0.5;
    };
}
```

## 视图系统

引擎引入视图组件解耦相机与渲染后端：

```tsx
export component View {
    position: vec3;
    rotation: quat;
    fov: float = 60.0;
    near: float = 0.1;
    far: float = 1000.0;
    render_path: RenderPath;
}

export enum RenderPath {
    Forward,
    Deferred,
    Custom(string),
}
```

不同的渲染后端可以独立解释同一个 `View`：
- 光栅化后端将其用作 MVP 矩阵
- 神经渲染后端将其用作射线采样的起点

## 未来扩展

虽然当前重点在光栅化，但架构已为以下前沿渲染技术预留了扩展能力。

### 神经渲染后端 (NeRF / 3D Gaussian Splatting)

- **数据表示**：引入 `NeuralVolume` 或 `GaussianSplat` 组件，指向预处理好的模型权重或 splat 数据资产
- **渲染接口**：后端实现特定的 `RenderView` 方法，执行射线行进 (NeRF) 或 Splat 光栅化 (3DGS)，并将结果直接写入 RHI 提供的纹理句柄
- **混合**：可与光栅化后端通过深度缓冲区混合，实现传统几何与神经表示的融合

```tsx
export component NeuralVolume {
    model: Handle<NeRFModel>;
    quality: Quality;
}

export component GaussianSplat {
    data: Handle<SplatData>;
    count: u32;
}
```

### 端到端扩散模型后端

- **交互模式**：此类后端不直接渲染几何体，而是根据 `View` 组件的观测参数以及文本/图像提示词（通过专用组件提供），在每一帧或按需生成像素输出
- **实现路径**：后端集成本地推理运行时（如 ONNX Runtime），接收引擎传递的场景元数据（如语义分割、深度缓冲区），引导扩散模型生成符合当前视角的图像

### 混合渲染后端

- **协调器**：实现一个特殊的 **`HybridRenderBackend`**，它不直接执行绘制，而是根据场景标记将渲染任务**分发给多个子后端**
- **合成**：负责收集各子后端的输出纹理，并执行最终合成（例如，将光栅化的 UI 叠加到神经渲染的场景上）

```tsx
export component HybridRender {
    primary_backend: RenderBackend;
    secondary_backend: RenderBackend;
    compose_mode: ComposeMode;
}
```

### 未来展望

| 技术 | 描述 |
| :--- | :--- |
| GPU 驱动管线 | 将剔除、LOD 选择等逻辑上移至 GPU，通过 `gg-shader` 的计算着色器实现 |
| 光线追踪支持 | `RHI` 抽象层将扩展以支持光线追踪加速结构和着色器绑定表 |
| 分布式渲染 | 探索将神经渲染后端部署在云端，通过 WebRTC 将像素流传输至客户端 |
| 移动端优化 | 针对 Vulkan Mobile 与 Metal 进行深度特化 |

## 调试工具

### 性能 HUD

- 帧时间统计
- Draw call 计数
- 内存使用情况
- ECS 系统耗时

### RenderDoc 集成

```tsx
// 编辑器中一键捕获
function capture_frame() {
    renderdoc.capture_next_frame();
}
```

### 着色器热重载

编辑器支持 `.shader` 文件的热重载，修改后自动重新编译并应用。

## 渲染管线优化

gg 引擎从现代 UI 框架（Flutter、Qt、Chrome）与 3D 渲染引擎中提炼出五项核心优化策略，并与引擎自身的 MSP、ECS、gg-shader 元编程等架构深度协同。

### 优化策略总览

| 策略 | 核心原理 | UI 渲染应用 | 3D 渲染应用 |
| :--- | :--- | :--- | :--- |
| PSO 预生成 | 构建时扫描渲染原语，元编程生成特化着色器 | Widget 渲染原语 → 零运行时着色器编译 | 材质变体 → 零运行时管线状态创建 |
| 批处理重排序 | ECS System 对 Renderable 组件排序 | Canvas 内元素按纹理/材质分组 | 实例化渲染按材质/网格分组 |
| 保留模式渲染 | 声明式生成不可变命令快照，仅差异更新 | Widget RenderObject 树 → CPU 零网格构建 | Command Table 跨帧复用 |
| Uber Shader + SDF | 宏展开消除分支，SDF 统一视觉表现 | UI Uber Shader → 单着色器渲染全 UI | 材质变体 Uber Shader → 减少 PSO 切换 |
| GPU 驱动动画 | Uniform 传递动画参数，顶点着色器完成位移 | 血条/冷却动画 → 零 CPU 顶点缓冲更新 | 风场植被/布料 → 零 CPU 参与 |

### PSO 预生成

**核心问题**：若运行时动态编译着色器变体，会导致类似 Flutter 早期 Skia 的"着色器编译卡顿"。

**gg 引擎解法**：利用 MSP 阶段一，将着色器变体生成从运行时提前至构建时。

**UI 渲染场景**：`gg_compiler` 扫描所有 Widget 代码中的渲染原语（如 `<rounded_rect radius=10>`、`<drop_shadow offset=5>`），自动为已知组合生成对应的 `.ggs` 着色器模块，利用 gg-shader 的 `<% %>` 元编程生成极简、无分支的特化版本。所有 PSO 在游戏启动时一次性创建完毕。

**3D 渲染场景**：同理，`gg_compiler` 扫描所有材质定义中的属性组合（如 `PBR + NORMAL_MAP + EMISSIVE`），为每种有效组合预生成 PSO。运行时不再需要任何着色器编译或管线状态创建。

```rust
// gg_compiler 在构建时扫描渲染原语后，自动生成的特化着色器示例

micro ps_rounded_rect_shadow(uv: vec2, size: vec2, radius: f32, shadow_offset: f32) -> vec4 {
    let d = sd_rounded_box(uv, size, radius);
    let shadow_d = sd_rounded_box(uv + vec2(shadow_offset, shadow_offset), size, radius);
    let color = smoothstep(0.0, 1.0, -d);
    let shadow = smoothstep(0.0, 2.0, -shadow_d) * 0.5;
    return vec4(color, color, color, 1.0) + vec4(0.0, 0.0, 0.0, shadow);
}
```

### 批处理重排序

**核心问题**：UI 的层级结构和 3D 场景的材质多样性，天然导致频繁的纹理与材质切换，造成 Draw Call 激增。

**gg 引擎解法**：不为每个渲染对象维护独立的渲染状态，而是利用 ECS 的 `System` 对渲染组件进行视图重组。

**统一抽象**：定义 `Renderable` 组件，包含 `z_order`、`texture_handle`、`material_type`。`UIBatchingSystem`（UI Canvas）和 `SceneBatchingSystem`（3D 场景）均按 `texture_handle` -> `material_type` -> `z_order` 进行基数排序。

```tsx
component Renderable {
    z_order: i32;
    texture_handle: Handle<Texture>;
    material_type: MaterialType;
}

system UIBatchingSystem {
    query = Query.all(Renderable);

    on_update(delta: f32) {
        <% var sorted = query.sort_by(r => (r.texture_handle, r.material_type, r.z_order)); %>
        <% foreach (var (renderable) in sorted) { %>
            emit_draw_command(renderable);
        <% } %>
    }
}
```

**效果**：原本穿插绘制的元素被重新分组，Draw Call 从 O(N) 降至 O(1) 纹理切换次数。

### 保留模式渲染

**核心问题**：即时模式重绘（如 Unity 的 OnGUI）导致 CPU 重复构建网格数据。

**gg 引擎解法**：采用声明式 + 保留模式，与 gg 的宏展开结合。

**机制**：
- `render()` 函数并非执行绘制，而是返回一个轻量级的 `RenderObject` 树
- 渲染后端持有这棵树的持久化版本
- 当属性变更时，引擎仅更新差异部分，CPU 零网格构建开销
- RHI 的 `Command Table` 支持跨帧复用，未变更的绘制命令无需重新录制

```vue
<script setup>
defineProps({ text: String });
</script>

<template>
    <!-- render() 仅在 text 变化时执行，生成不可变的绘制命令快照 -->
    <rect color=0xFFFFFF />
    <text :value="text" />
</template>
```

### Uber Shader 与 SDF

**核心问题**：UI 需要大量圆角、边框、抗锯齿逻辑，3D 材质需要大量变体分支，手写分支会降低 GPU 占用率。

**gg 引擎解法**：在 gg-shader 标准库中内置 Uber Shader 模板，通过宏展开生成特化版本消除运行时分支。

**UI Uber Shader**：所有 Widget 的视觉表现（圆角、渐变、阴影）通过有符号距离函数 (SDF) 在像素着色器中解析，不同 Widget 仅传递不同的 Uniform 参数块。

```rust
micro ps_ui_uber(uv: vec2, size: vec2, params: UIParams) -> vec4 {
    let d = sd_rounded_box(uv, size, params.border_radius);
    let color = params.fill_color;

    <% if (widget.has_border) { %>
        let border_dist = sd_rounded_box(uv, size, params.border_radius);
        color = mix(color, params.border_color, smoothstep(params.border_width - 1.0, params.border_width, -border_dist));
    <% } %>

    let aa = smoothstep(0.0, 1.0, -d);
    return vec4(color.rgb, color.a * aa);
}
```

**3D 材质变体扩展**：同理，3D 渲染的 PBR Uber Shader 通过 `<% %>` 元编程为每种材质属性组合生成特化版本，运行时零分支开销。

### GPU 驱动动画

**核心问题**：血条伸缩、技能冷却转圈、风场植被摆动这类高频动画，若每帧在 CPU 更新顶点缓冲，会消耗大量带宽。

**gg 引擎解法**：将动画参数作为 Uniform 传递，在顶点着色器中完成顶点位移。

```rust
[Vertex]
micro vs_ui(input: UIVertex, [Material] mat: UIMaterial) -> vec4 {
    let scale = 1.0 + sin(global_time * mat.pulse_speed) * mat.pulse_strength;
    let pos = vec4(input.position * scale, 1.0);
    return mat.mvp * pos;
}
```

**效果**：CPU 仅更新一个 Uniform 值，GPU 端完成全部顶点变换，零 CPU 顶点缓冲更新开销。

## 最佳实践

### 性能优化

- 使用实例化渲染减少 draw call
- 合理使用 LOD 降低几何复杂度
- 使用遮挡剔除减少过度绘制
- 合并材质减少状态切换

### 资源管理

- 使用纹理压缩减少显存占用
- 使用 Mipmap 提高渲染质量
- 异步加载资源避免卡顿

## 下一步

- 阅读 [gg-shader 指南](gg-shader.md) 学习着色器编写
- 阅读 [架构设计](architecture.md) 了解整体架构
- 查看 [示例项目](../../examples/) 了解实际用法
