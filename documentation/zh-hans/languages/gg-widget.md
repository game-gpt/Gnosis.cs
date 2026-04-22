# gg-widget 语言语法指南

本文档介绍 gg-widget 语言的语法特性和使用方法。

## ⚠️ 语言归属声明

**gg-widget 是游戏对象语言，不是 C#。** 编辑器 UI 使用 gg-widget 编写，**而非 C#**：

- 编辑器本身完全由 GG 语言族编写，运行于 gg 虚拟机之上。
- gg-widget 的 `<script setup>` 块使用 GG 语言的类型系统（`f64`、`string`、`bool`），**不是 JavaScript/TypeScript**。
- Widget 的样式属性（如 `style="bg-blue-500"`）是 HTML 标签属性，**不是 GG 语言的特性标注 (Attribute)**，也不是 C# 的属性 (Property)。

详见 [项目介绍 - 三层蛋糕模型](../overview/introduction.md#⚠️-关键概念三层蛋糕模型)。

## ⚠️ 关键区分：Widget 与 Game UI

在深入 gg-widget 语言之前，必须首先理解 gg 引擎中 **Widget** 与 **Game UI** 是两套完全不同的 UI 体系，尽管它们共享同一底座（gg 虚拟机 + RHI 渲染后端）。

### 为什么需要两套 UI 体系？

游戏 UI 与通用 Widget 体系在需求上存在**根本性差异**：

| 维度 | 通用 Widget（编辑器） | 游戏 UI（Game UI） |
| :--- | :--- | :--- |
| **核心诉求** | 高保真、低延迟即时响应、复杂布局嵌套 | 性能极致、与 3D 渲染管线深度集成 |
| **渲染频率** | 按需重绘（Dirty Rect） | 每帧强制重绘（VSync） |
| **数据结构** | 持久化的 RenderObject 树 | ECS 组件 + 流式顶点缓冲 |
| **与 3D 关系** | 完全隔离，浮于顶层 | 深度融合，可接受光照/后处理 |
| **批处理策略** | 空间划分（Tile） | 材质/图集排序（Canvas） |
| **动画更新** | CPU 逻辑驱动 Widget 属性 | GPU Uniform 驱动 Shader 参数 |
| **空间存在** | 仅屏幕空间 | 屏幕空间 + 世界空间 |
| **材质融合** | 不需要 | UI 元素可接受光照、溶解、扭曲等后期特效 |

### 架构上的明确分离

gg 引擎在架构上对二者进行了**明确的分离**：

- **`widget` 模块**：服务于编辑器，走**独立于游戏循环的 ImGUI 式绘制**。开发者使用 `gg-widget` 语言以类 React 声明式方式编写编辑器面板。
- **`game_ui` 模块**：服务于游戏运行时，走 **ECS + 材质系统 + 批处理优化**的路径。开发者使用 `gg-script` 定义 ECS 组件和系统来构建游戏 UI。

```
┌─────────────────────────────────────────────────────────────┐
│                     共享底座                                  │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐                 │
│  │ gg 虚拟机 │   │ RHI 后端  │   │ gg-shader │                │
│  └──────────┘   └──────────┘   └──────────┘                 │
└─────────────────────────────────────────────────────────────┘
           ↗                                    ↖
┌────────────────────┐            ┌────────────────────────────┐
│  Widget 模块（编辑器）│            │  Game UI 模块（游戏运行时）  │
│  - 声明式 Widget 树  │            │  - ECS 组件 + 系统          │
│  - 保留模式渲染      │            │  - Canvas 批处理            │
│  - 按需重绘          │            │  - 每帧强制重绘              │
│  - 屏幕空间          │            │  - 屏幕/世界空间            │
│  - SDF Uber Shader  │            │  - 图集 + Uber Shader       │
└────────────────────┘            └────────────────────────────┘
```

> **本文档聚焦于 `widget` 模块**（编辑器 UI）。关于游戏运行时 UI 的详细设计，请参阅 [Game UI 系统](#game-ui-系统) 章节。

## 语言概述

gg-widget 是 gg 引擎**编辑器 UI** 的构建语言，具有以下特性：

| 特性          | 描述                |
| :---------- | :---------------- |
| 组件化开发       | 支持组件的定义和复用     |
| 模板语法        | 与 Vue 3 语法兼容     |
| 样式系统        | 支持 Tailwind 语法的样式 |
| 响应式数据       | 支持数据绑定和响应式更新   |
| 事件处理        | 支持组件事件和用户交互    |

## 基础语法

### 组件定义

```vue
<script setup>
defineProps({
  text: String,
  disabled: {
    type: Boolean,
    default: false
  }
});

const emit = defineEmits(['click']);

const handleClick = () => {
  emit('click');
};
</script>

<template>
  <Button 
    style="bg-blue-500 hover:bg-blue-600 text-white font-bold py-2 px-4 rounded"
    :disabled="disabled"
    @click="handleClick"
  >
    {{ text }}
  </Button>
</template>
```

### 数据绑定

```vue
<script setup>
import { ref } from 'vue';

const title = ref('Hello');
const containerClass = ref('p-4');
const imageUrl = ref('https://example.com/image.jpg');
const showContent = ref(true);
const items = ref([
  { name: 'Item 1' },
  { name: 'Item 2' },
  { name: 'Item 3' }
]);
</script>

<template>
  <!-- 文本绑定 -->
  <Text>{{ title }}</Text>
  
  <!-- 属性绑定 -->
  <HStack :style="containerClass"></HStack>
  <Image :src="imageUrl" />
  
  <!-- 条件渲染 -->
  <template v-if="showContent">
    <VStack>显示内容</VStack>
  </template>
  <template v-else>
    <VStack>隐藏内容</VStack>
  </template>
  
  <!-- 循环渲染 -->
  <VStack>
    <VStack v-for="item in items" :key="item.name">
      {{ item.name }}
    </VStack>
  </VStack>
</template>
```

### 事件处理

```vue
<script setup>
const handleClick = () => {
  console.log('Button clicked');
};

const handleInput = (event) => {
  console.log('Input value:', event.target.value);
};

const handleCustomEvent = (data) => {
  console.log('Custom event data:', data);
};
</script>

<template>
  <!-- 点击事件 -->
  <Button @click="handleClick">点击我</Button>
  
  <!-- 输入事件 -->
  <Input @input="handleInput" />
  
  <!-- 自定义事件 -->
  <CustomComponent @custom-event="handleCustomEvent" />
</template>
```

## 组件示例

### 基础组件

```vue
<script setup>
defineProps({
  name: {
    type: String,
    default: 'World'
  }
});
</script>

<template>
  <VStack style="align-center">
    <Text style="text-2xl font-bold">Hello, {{ name }}!</Text>
    <Text style="text-gray-600">Welcome to gg-widget</Text>
  </VStack>
</template>
```

### 状态管理

```vue
<script setup>
import { ref } from 'vue';

defineProps({
  initialValue: {
    type: Number,
    default: 0
  }
});

const count = ref(initialValue);

const increment = () => {
  count.value += 1;
};

const decrement = () => {
  count.value -= 1;
};
</script>

<template>
  <HStack style="align-center space-x-4">
    <Button 
      style="bg-gray-200 px-4 py-2 rounded"
      @click="decrement"
    >
      -
    </Button>
    <Text style="text-xl">{{ count }}</Text>
    <Button 
      style="bg-blue-500 text-white px-4 py-2 rounded"
      @click="increment"
    >
      +
    </Button>
  </HStack>
</template>
```

### 复合组件

```vue
<!-- TodoList.vue -->
<script setup>
defineProps({
  todos: Array
});

const emit = defineEmits(['toggle', 'delete']);

const handleToggle = (id) => {
  emit('toggle', id);
};

const handleDelete = (id) => {
  emit('delete', id);
};
</script>

<template>
  <VStack style="space-y-2">
    <TodoItem 
      v-for="todo in todos" 
      :key="todo.id"
      :todo="todo"
      @toggle="handleToggle"
      @delete="handleDelete"
    />
  </VStack>
</template>

<!-- TodoItem.vue -->
<script setup>
defineProps({
  todo: Object
});

const emit = defineEmits(['toggle', 'delete']);

const handleToggle = () => {
  emit('toggle', todo.id);
};

const handleDelete = () => {
  emit('delete', todo.id);
};
</script>

<template>
  <HStack style="align-center space-x-2 p-2 border rounded">
    <Checkbox 
      :checked="todo.completed"
      @change="handleToggle"
    />
    <Text :style="todo.completed ? 'text-gray-400 line-through' : ''">
      {{ todo.text }}
    </Text>
    <Button 
      style="ml-auto text-red-500 hover:text-red-700"
      @click="handleDelete"
    >
      删除
    </Button>
  </HStack>
</template>
```

## 样式系统

### Tailwind 语法

gg-widget 支持 Tailwind 语法，使用 `style` 属性：

```vue
<template>
  <!-- 基础样式 -->
  <VStack style="bg-white p-4 rounded shadow">内容</VStack>
  
  <!-- 响应式样式 -->
  <VStack style="md:flex md:items-center md:justify-between">内容</VStack>
  
  <!-- 悬停效果 -->
  <Button style="hover:bg-blue-600 transition-colors">按钮</Button>
  
  <!-- 条件样式 -->
  <VStack :style="isActive ? 'bg-blue-500 text-white' : 'bg-gray-200'">内容</VStack>
</template>
```

### 样式组合

```vue
<script setup>
defineProps({
  title: String,
  content: String
});
</script>

<template>
  <VStack style="border rounded overflow-hidden shadow">
    <VStack style="bg-gray-100 p-4 border-b">
      <Text style="font-bold">{{ title }}</Text>
    </VStack>
    <VStack style="p-4">
      {{ content }}
    </VStack>
  </VStack>
</template>
```

## 生命周期

```vue
<script setup>
import { ref, onMounted, onUpdated, onUnmounted } from 'vue';

const message = ref('Hello');

onMounted(() => {
  console.log('组件挂载');
});

onUpdated(() => {
  console.log('组件更新');
});

onUnmounted(() => {
  console.log('组件卸载');
});
</script>

<template>
  <VStack>{{ message }}</VStack>
</template>
```

## 状态管理

### 全局状态

```vue
<!-- store.js -->
import { reactive } from 'vue';

export const AppStore = reactive({
  state: {
    user: null,
    theme: "light"
  },
  actions: {
    setUser(user) {
      this.state.user = user;
    },
    toggleTheme() {
      this.state.theme = this.state.theme === "light" ? "dark" : "light";
    }
  }
});

<!-- UserProfile.vue -->
<script setup>
import { AppStore } from './store.js';
</script>

<template>
  <VStack :style="AppStore.state.theme === 'dark' ? 'bg-gray-900 text-white' : 'bg-white text-gray-900'">
    <template v-if="AppStore.state.user">
      <Text>Welcome, {{ AppStore.state.user.name }}!</Text>
    </template>
    <template v-else>
      <Text>Please login</Text>
    </template>
    <Button @click="AppStore.actions.toggleTheme">
      Toggle Theme
    </Button>
  </VStack>
</template>
```

## 与引擎集成

### 在 gg 脚本中使用组件

```tsx
# main.script
import ui;

micro main() {
  let app = ui.create_component(App);
  ui.mount(app, ui.root);
}
```

### 与 ECS 集成

```tsx
# game.script
import ui;

component UI {
  root_component: Component;
}

system UISystem {
  query = Query.all(UI);
  
  on_load() {
    <% foreach (var (ui) in query) { %>
      ui.root_component = ui.create_component(MainMenu);
      ui.mount(ui.root_component, ui.root);
    <% } %>
  }
  
  on_update(delta: f32) {
    # 更新 UI
  }
}
```

## UI 渲染架构

gg 引擎的 Widget 体系从现代 UI 框架（Flutter、Qt、Chrome）中提炼出五项核心设计思想，并与引擎自身的 MSP、ECS、gg-shader 元编程等架构深度协同，实现开发体验与运行时性能的双重突破。

### 设计思想总览

| 借鉴思想 | 来源 | gg 引擎落地方案 | 核心优势 |
| :--- | :--- | :--- | :--- |
| 着色器预编译 | Flutter (Impeller) | 构建时扫描生成 PSO | 零运行时卡顿 |
| 场景图重排序 | Qt | ECS System 对 Renderable 排序 | Draw Call 数量最小化 |
| 保留模式绘制 | Chrome / Skia | 声明式 Widget + 命令表 | CPU 网格构建零开销 |
| UI Uber Shader | 现代图形学 | SDF 驱动 + 宏展开特化 | 单着色器渲染全 UI |
| 模块级热重载 | Web (HMR) | gg 字节码模块替换 | 状态保持的毫秒级预览 |

### 构建时 PSO 预生成

gg 引擎利用多阶段编程 (MSP) 特性，在构建时扫描所有 Widget 渲染原语并自动生成对应的特化着色器模块，使所有 PSO 在游戏启动时一次性创建完毕，彻底消除运行时着色器编译卡顿。

> 详细机制与代码示例请参阅 [渲染管线优化 - PSO 预生成](../development/rendering.md#pso-预生成)。

### 基于 ECS 的场景图批处理重排序

gg 引擎不为每个 Widget 维护独立的渲染状态，而是利用 ECS 的 System 对所有 Renderable 组件按纹理、材质、Z 序进行基数排序，将原本穿插绘制的 UI 元素重新分组，使 Draw Call 从 O(N) 降至 O(1) 纹理切换次数。

> 详细机制与代码示例请参阅 [渲染管线优化 - 批处理重排序](../development/rendering.md#批处理重排序)。

### 声明式 Widget 与保留模式渲染

Widget 体系采用声明式 + 保留模式渲染：`render()` 函数返回轻量级的 `RenderObject` 树而非执行绘制，UI 渲染后端持有该树的持久化版本，仅在数据变更时做差异更新，CPU 零网格构建开销。

> 详细机制与代码示例请参阅 [渲染管线优化 - 保留模式渲染](../development/rendering.md#保留模式渲染)。

### UI Uber Shader：SDF 驱动 + 宏展开特化

gg-shader 标准库内置专为 UI 优化的 `ui_uber` 着色器模板，所有 Widget 的视觉表现通过有符号距离函数 (SDF) 在像素着色器中解析，不同 Widget 仅传递不同的 Uniform 参数块，绝大多数 UI 元素共享同一着色器实例，GPU 占用率极高。

> 详细机制与代码示例请参阅 [渲染管线优化 - Uber Shader 与 SDF](../development/rendering.md#uber-shader-与-sdf)。

### Widget 级热重载

利用 gg 字节码的模块化特性，每个 `.gg` UI 文件被编译为独立的 `.ggc` 模块，编辑器检测到文件变更时仅替换虚拟机中对应模块的表条目，下一帧即生效且输入框文字、滚动位置等状态不丢失。

> 详细机制与代码示例请参阅 [热更新系统](../development/hot-update.md)。

## 最佳实践

### 组件设计

- 组件应该小而专注，只负责一个功能
- 使用 props 传递数据，使用 events 传递事件
- 合理使用插槽（slots）提高组件灵活性

### 样式最佳实践

- 优先使用 Tailwind 类名，避免内联样式
- 使用语义化的类名，提高代码可读性
- 合理使用响应式断点，适配不同设备

### 性能优化

- 避免不必要的渲染，使用 computed 属性
- 合理使用 key 属性，优化列表渲染
- 使用虚拟列表处理大量数据
- 优先使用内置渲染原语（`<rect>`、`<text>`、`<rounded_rect>`），确保编译器能正确推导 PSO
- 避免在 `render()` 中创建临时对象，保持 `RenderObject` 树的稳定性以充分利用保留模式
- 合理规划 Widget 的纹理资源，使 `UIBatchingSystem` 能最大化批处理效果

## Game UI 系统

> 本章节描述 gg 引擎的**游戏运行时 UI** 系统。如前所述，Game UI 与 Widget 是两套完全独立的体系。

游戏 UI（HUD、血条、技能轮盘、世界空间漂浮文字）的核心要求与编辑器 Widget 截然不同：

1. **性能极致**：必须与 3D 渲染管线深度集成，不能打断 GPU 工作流
2. **材质融合**：UI 元素常需接受光照、溶解、扭曲等后期特效
3. **空间存在**：UI 既可以屏幕空间（Screen Space），也可以世界空间（World Space）附着于角色
4. **动画驱动**：受游戏逻辑（受伤闪红、能量充能）驱动的动态材质参数

以下是从主流游戏引擎中提炼的、融入 gg 游戏 UI 系统的核心设计思想。

### Canvas 批处理与图集打包

**核心问题**：游戏 UI 由成百上千个小图片（九宫格边框、图标、字体字符）组成，若每个元素单独绘制，Draw Call 将瞬间爆炸。

**gg 解法**：

- **静态图集（Atlas）**：资产管线在**负一阶段**自动将标记为 `ui_sprite` 的纹理打包进超大图集，并生成对应的 UV 映射表
- **ECS 批处理视图**：在 `game_ui` 系统中，定义 `CanvasRenderer` 组件。一个 Canvas 节点下的所有 UI 元素共享同一个材质（图集纹理 + Uber Shader）。`UIRenderSystem` 每帧遍历 Canvas 树，将顶点数据**流式写入环形缓冲区**，一次 Draw Call 绘制整个 Canvas

```tsx
export component Canvas {
    render_mode: enum { ScreenSpace, WorldSpace };
    sorting_order: int;
}

export component UIElement {
    color: vec4;
    atlas_uv: rect;
    size: vec2;
}
```

### WidgetComponent 与世界空间融合

**核心问题**：游戏需要将 UI 放置在 3D 世界中（如怪物头顶血条、路标），并且 UI 需要接收光照或产生阴影。

**gg 解法**：

- **WidgetComponent**：这是一种特殊的 ECS 组件，它可以将一个完整的 UI Canvas 渲染到一张**渲染目标纹理（Render Target）**上
- **材质映射**：这张纹理被动态赋值给一个 3D 材质的 `Emissive` 或 `BaseColor` 通道。这意味着 UI 可以接受场景光照、被后期特效（如景深）模糊
- **输入穿透**：通过 `ViewportRaycaster` 系统，将鼠标/触屏射线投射到 WidgetComponent 的包围盒上，并将屏幕坐标转换为 UI 局部坐标进行交互

```tsx
export component WidgetComponent {
    canvas_asset: AssetHandle<Canvas>;
    render_texture: AssetHandle<RenderTexture>;
    resolution: vec2 = vec2(512, 256);
    receive_lighting: bool = true;
}
```

### 基于 Theme 与 StyleBox 的即时变体

**核心问题**：游戏 UI 的风格（如按钮悬停、按下、禁用）变化极其频繁，但底层几何结构完全一致。

**gg 解法**：

利用 **`gg-shader` 元编程 + 材质参数块**，避免为每种状态创建新材质实例。

- **StyleBox 数据结构**：不是单独的纹理，而是一组描述边框、颜色、内边距的**纯数据**
- **Uber UI Shader 特化**：在构建时，`gg_compiler` 扫描项目中用到的所有 StyleBox 类型（如 `hover`、`pressed`、`disabled`），并在 `game_ui.shader` 着色器中生成对应的**静态变体数组**。运行时只需传递一个 `style_id` 整数，GPU 即可通过索引获取绘制参数，**零状态切换开销**

### 游戏 UI 专属的"无干扰"输入路由

**核心问题**：当玩家点击"攻击"按钮时，这个点击**绝不能**穿透 UI 导致角色向该方向移动。

**gg 解法**：

在 ECS 的输入处理管线中插入一个 **`UILayerMask` 过滤阶段**：

```tsx
system InputRouting {
    on_event(event: InputEvent) {
        let consumed = ui_manager.process_event(event);

        if (!consumed) {
            game_world.send_event(event);
        }
    }
}
```

这一机制保证了 UI 交互与 3D 场景交互的**严格互斥**，无需开发者在每个游戏逻辑中手动判断"鼠标是否在 UI 上"。

### 性能压榨：顶点着色器驱动的动画

**核心问题**：血条伸缩、技能冷却转圈这类高频动画，若每帧在 CPU 更新顶点缓冲，会消耗大量带宽。

**gg 解法**：

将动画参数作为 **Uniform** 传递，在**顶点着色器（Vertex Shader）**中完成顶点位移：

```rust
[Vertex]
micro vs_ui(input: UIVertex, [Material] mat: UIMaterial) -> vec4 {
    let scale = 1.0 + sin(global_time * mat.pulse_speed) * mat.pulse_strength;
    let pos = vec4(input.position * scale, 1.0);
    return mat.mvp * pos;
}
```

### Game UI 设计哲学总结

| 维度 | 通用 Widget（编辑器） | 游戏 UI（Game UI） |
| :--- | :--- | :--- |
| **渲染频率** | 按需重绘（Dirty Rect） | 每帧强制重绘（VSync） |
| **数据结构** | 持久化的 RenderObject 树 | ECS 组件 + 流式顶点缓冲 |
| **与 3D 关系** | 完全隔离，浮于顶层 | 深度融合，可接受光照/后处理 |
| **批处理策略** | 空间划分（Tile） | 材质/图集排序（Canvas） |
| **动画更新** | CPU 逻辑驱动 Widget 属性 | GPU Uniform 驱动 Shader 参数 |
| **输入路由** | 编辑器焦点管理 | UILayerMask 严格互斥 |
| **空间模式** | 仅屏幕空间 | 屏幕空间 + 世界空间 |

通过这种清晰的分层，gg 引擎既能保证编辑器 **Widget** 的复杂交互逻辑易于编写（类 React 声明式），又能确保运行时 **游戏 UI** 的渲染性能达到 3A 级标准。

## 下一步

- 阅读 [编辑器架构](../development/editor.md) 了解 Widget 在编辑器中的使用
- 阅读 [gg-shader 指南](gg-shader.md) 了解着色器编写
- 查看 [示例项目](../../examples/) 了解实际用法