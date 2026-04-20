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
┌─────────────┐
│  UI pass    │ → Widget 渲染
└─────────────┘
```

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
.ggs 源码
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

- **前端 (纯 C#)**：解析 `.ggs` 源码，生成抽象语法树 (AST)，执行元编程展开，产出着色器中间表示 (IR)
- **后端 (纯 C#)**：基于 `Spv.Generator` 等库，直接从 IR 生成符合 Vulkan 规范的 SPIR-V 字节码
- **集成 (C# Source Generator)**：在 MSBuild 过程中，将生成的 SPIR-V 字节码以 `ReadOnlySpan<byte>` 形式嵌入 C# 源码，实现编译时资产嵌入，保证 AOT 兼容性

### 材质系统

```tsx
export material PBRMaterial {
    vertex_shader: "shaders/pbr_vertex.ggs";
    fragment_shader: "shaders/pbr_fragment.ggs";
    
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

编辑器支持 `.ggs` 文件的热重载，修改后自动重新编译并应用。

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
