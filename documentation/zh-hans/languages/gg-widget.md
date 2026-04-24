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

## 布局系统规范

GGWidget 布局系统基于 **Measure-Arrange 两遍布局协议**，与 Flutter/WPF 的布局模型一致。所有布局容器均为模板中的元素标签，通过属性配置布局行为。

### 布局容器一览

| 容器标签 | 布局模型 | 说明 |
| :--- | :--- | :--- |
| `Flex` | Flexbox | 弹性盒布局，最通用的布局容器 |
| `HBox` | Flex (Row) | 水平排列语法糖 |
| `VBox` | Flex (Column) | 垂直排列语法糖 |
| `Grid` | CSS Grid | 行列网格布局，支持附加属性 |
| `Dock` | Dock | 停靠布局，类似 WPF DockPanel |
| `Stack` | 层叠 | 所有子元素从同一原点排列 |
| `Wrap` | 流式换行 | 子元素超出主轴空间时自动换行 |
| `ClipRect` | 裁剪 | 裁剪子元素溢出内容 |
| `Flexible` | 弹性子项 | 在 Flex 中按比例分配剩余空间 |
| `Expanded` | 扩展子项 | Flexible 的 Tight 变体，填满剩余空间 |

### Flex 布局

`Flex` 是最核心的布局容器，对应 CSS Flexbox 模型。`HBox` 和 `VBox` 是其语法糖。

```vue
<template>
  <!-- 水平弹性布局 -->
  <Flex direction="row" main-align="space-between" cross-align="center" spacing="8">
    <Text>左侧</Text>
    <Text>右侧</Text>
  </Flex>

  <!-- HBox = Flex direction="row" -->
  <HBox spacing="4">
    <Text>A</Text>
    <Text>B</Text>
  </HBox>

  <!-- VBox = Flex direction="column" -->
  <VBox spacing="4">
    <Text>A</Text>
    <Text>B</Text>
  </VBox>
</template>
```

#### Flex 属性

| 属性 | 类型 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- |
| `direction` | `row` \| `column` \| `row-reverse` \| `column-reverse` | `row` | 主轴方向 |
| `main-align` | `start` \| `center` \| `end` \| `space-between` \| `space-around` \| `space-evenly` | `start` | 主轴对齐 |
| `cross-align` | `start` \| `center` \| `end` \| `stretch` | `start` | 交叉轴对齐 |
| `spacing` | `float` | `0` | 子元素间距 |
| `wrap` | `no-wrap` \| `wrap` \| `wrap-reverse` | `no-wrap` | 换行模式 |

#### Flexible / Expanded

在 Flex 容器内，`Flexible` 和 `Expanded` 子元素按 `flex` 权重分配剩余空间：

```vue
<template>
  <HBox>
    <Text>固定宽度</Text>
    <Expanded flex="1">
      <Text>占据剩余空间</Text>
    </Expanded>
    <Expanded flex="2">
      <Text>占据 2/3 剩余空间</Text>
    </Expanded>
  </HBox>
</template>
```

| 属性 | 类型 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- |
| `flex` | `int` | `1` | 弹性权重 |

> `Expanded` 等价于 `Flexible` 的 `fit="tight"` 模式，强制填满分配空间；`Flexible` 默认 `fit="loose"`，子元素可以小于分配空间。

### Grid 布局

`Grid` 提供行列网格布局，支持附加属性定位子元素。

```vue
<template>
  <Grid
    rows="auto 1* 200px"
    columns="1* 2*"
    row-spacing="4"
    column-spacing="8"
  >
    <Text row="0" column="0">第 1 行第 1 列</Text>
    <Text row="0" column="1">第 1 行第 2 列</Text>
    <Text row="1" column="0" row-span="1" column-span="2">跨两列</Text>
    <Text row="2" column="0">固定高度行</Text>
  </Grid>
</template>
```

#### Grid 行列定义语法

`rows` 和 `columns` 属性使用空格分隔的尺寸列表，每个尺寸支持三种单位：

| 语法 | 单位类型 | 说明 |
| :--- | :--- | :--- |
| `auto` | Auto | 根据子元素内容自动确定尺寸 |
| `N*` | Star | 按比例分配剩余空间（如 `1*`、`2*`） |
| `Npx` | Pixel | 固定像素尺寸（如 `200px`） |

示例：`rows="auto 1* 200px"` 表示第一行自适应、第二行按比例、第三行固定 200px。

#### Grid 附加属性

附加属性以元素属性形式写在子元素上：

| 属性 | 类型 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- |
| `row` | `int` | `0` | 所在行索引 |
| `column` | `int` | `0` | 所在列索引 |
| `row-span` | `int` | `1` | 行跨越数 |
| `column-span` | `int` | `1` | 列跨越数 |

### Dock 布局

`Dock` 布局将子元素停靠在容器的上/下/左/右边缘，最后一个子元素默认填充剩余空间。

```vue
<template>
  <Dock>
    <HBox dock="top" height="40">顶部工具栏</HBox>
    <VBox dock="left" width="200">左侧面板</VBox>
    <VBox dock="right" width="200">右侧面板</VBox>
    <VBox dock="bottom" height="24">底部状态栏</VBox>
    <VBox dock="fill">主内容区域</VBox>
  </Dock>
</template>
```

| 属性 | 类型 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- |
| `dock` | `top` \| `bottom` \| `left` \| `right` \| `fill` | `fill` | 停靠位置 |

> 布局顺序：Top → Bottom → Left → Right → Fill。Top/Bottom 消耗高度，Left/Right 消耗剩余宽度，Fill 占据所有剩余空间。

### Stack 布局

`Stack` 是层叠布局，所有子元素从同一原点排列，后添加的子元素覆盖先添加的。子元素可通过 `left`/`top` 属性偏移位置。

```vue
<template>
  <Stack>
    <Image src="background.png" />
    <Text left="10" top="20">叠加文字</Text>
  </Stack>
</template>
```

### Wrap 布局

`Wrap` 是流式换行布局，子元素沿主轴排列，超出可用空间时自动换行。

```vue
<template>
  <Wrap direction="horizontal" spacing="8" run-spacing="4">
    <Text>标签1</Text>
    <Text>标签2</Text>
    <Text>标签3</Text>
  </Wrap>
</template>
```

| 属性 | 类型 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- |
| `direction` | `horizontal` \| `vertical` | `horizontal` | 排列方向 |
| `spacing` | `float` | `0` | 同行元素间距 |
| `run-spacing` | `float` | `0` | 行间间距 |

### ClipRect

`ClipRect` 裁剪容器，将子元素内容限制在容器边界内。

```vue
<template>
  <ClipRect width="200" height="100">
    <ScrollView>
      <Text>很长的内容...</Text>
    </ScrollView>
  </ClipRect>
</template>
```

### 通用盒模型属性

所有布局容器和控件均支持以下盒模型属性：

| 属性 | 类型 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- |
| `margin` | `EdgeInsets` | `0` | 外边距 |
| `padding` | `EdgeInsets` | `0` | 内边距 |
| `border` | `EdgeInsets` | `0` | 边框宽度 |
| `border-color` | `Color` | `transparent` | 边框颜色 |
| `width` | `float?` | `null` | 显式宽度 |
| `height` | `float?` | `null` | 显式高度 |
| `min-width` | `float` | `0` | 最小宽度 |
| `min-height` | `float` | `0` | 最小高度 |
| `max-width` | `float` | `Infinity` | 最大宽度 |
| `max-height` | `float` | `Infinity` | 最大高度 |
| `background` | `Color` | `transparent` | 背景色 |
| `foreground` | `Color` | `white` | 前景色 |
| `visibility` | `visible` \| `hidden` \| `collapsed` | `visible` | 可见性 |
| `id` | `string?` | `null` | 元素标识（用于样式选择器 `#id`） |
| `class` | `string?` | `null` | 样式类名（用于样式选择器 `.class`） |

#### EdgeInsets 语法

`margin`/`padding`/`border` 支持 CSS 风格的简写语法：

| 写法 | 含义 |
| :--- | :--- |
| `"8"` | 四边均为 8 |
| `"8 16"` | 上下 8，左右 16 |
| `"4 8 12 16"` | 上 4、右 8、下 12、左 16 |

## 样式系统规范

GGWidget 样式系统采用 **SCSS 兼容语法**，在 `<style>` 块中声明样式规则。样式系统由 `Oak.Scss` 解析器提供解析支持，由 `Gnosis.Widget.Style` 运行时提供匹配和应用能力。

### `<style>` 块语法

```vue
<template>
  <VBox class="container">
    <Text class="title">标题</Text>
    <Text class="subtitle">副标题</Text>
  </VBox>
</template>

<style>
.container {
  background: var(--surface);
  padding: 16px;
  border: 1px solid var(--border);
  border-radius: 8px;
}

.title {
  font-size: 24px;
  font-weight: bold;
  color: var(--text);
}

.subtitle {
  font-size: 14px;
  color: var(--text);
  opacity: 0.6;
}
</style>
```

### 选择器

GGWidget 支持以下选择器类型，与 CSS/SCSS 选择器语义一致：

| 选择器 | 语法 | 说明 | 示例 |
| :--- | :--- | :--- | :--- |
| 类型选择器 | `TypeName` | 按元素类型匹配 | `Button { ... }` |
| ID 选择器 | `#id` | 按元素 ID 匹配 | `#main-panel { ... }` |
| 类选择器 | `.class` | 按样式类名匹配 | `.container { ... }` |
| 伪类选择器 | `:pseudo` | 按交互状态匹配 | `:hover { ... }` |
| 父引用选择器 | `&` | SCSS 嵌套中引用父选择器 | `&:hover { ... }` |

#### 支持的伪类

| 伪类 | 说明 |
| :--- | :--- |
| `:hover` | 鼠标悬停 |
| `:pressed` | 鼠标按下 |
| `:focus` | 获得焦点 |
| `:disabled` | 控件禁用 |

### SCSS 嵌套

`<style>` 块支持 SCSS 风格的嵌套规则，使用 `&` 引用父选择器：

```vue
<style>
.panel {
  background: var(--surface);
  padding: 16px;

  .title {
    font-size: 24px;
    font-weight: bold;
  }

  &:hover {
    background: var(--hover-overlay);
  }

  &:pressed {
    background: var(--pressed-overlay);
  }
}
</style>
```

### 特异性与优先级

样式匹配遵循 CSS 特异性算法：

```
特异性 = (ID 选择器数 << 16) | (类选择器数 + 伪类选择器数 << 8) | 类型选择器数
```

优先级从高到低：
1. `!important` 声明
2. 高特异性规则
3. 同特异性下后定义的规则覆盖先定义的
4. 内联 `style` 属性（最高优先级）

### CSS 属性映射

GGWidget 支持的 CSS 属性及其映射到 Widget 属性的对应关系：

| CSS 属性 | Widget 属性 | 值类型 |
| :--- | :--- | :--- |
| `background` / `background-color` | `Background` | Color |
| `foreground` / `color` | `Foreground` | Color |
| `border-color` | `BorderColor` | Color |
| `margin` | `Margin` | EdgeInsets |
| `padding` | `Padding` | EdgeInsets |
| `border` | `Border` | EdgeInsets |
| `width` | `Width` | float |
| `height` | `Height` | float |
| `min-width` | `MinWidth` | float |
| `min-height` | `MinHeight` | float |
| `max-width` | `MaxWidth` | float |
| `max-height` | `MaxHeight` | float |
| `visibility` | `Visibility` | `visible` \| `hidden` \| `collapsed` |

### 主题变量

GGWidget 支持通过 CSS 变量语法引用主题值，实现暗色/亮色主题切换：

```vue
<style>
.panel {
  background: var(--surface);
  color: var(--text);
  border: 1px solid var(--border);
}

.button {
  background: var(--primary);
  color: white;
}
</style>
```

#### 内置主题变量

| 变量 | 说明 |
| :--- | :--- |
| `--surface` | 表面色 |
| `--background` | 背景色 |
| `--border` | 边框色 |
| `--text` | 文本色 |
| `--primary` | 主色调 |
| `--accent` | 强调色 |
| `--error` | 错误色 |
| `--warning` | 警告色 |
| `--success` | 成功色 |
| `--hover-overlay` | 悬停叠加色 |
| `--pressed-overlay` | 按下叠加色 |
| `--selection` | 选中色 |

变量引用支持两种前缀：`var(--name)` 和 `$name`，两者等价。

### 颜色值语法

| 语法 | 说明 | 示例 |
| :--- | :--- | :--- |
| `#RGB` | 3 位十六进制 | `#F00` |
| `#RGBA` | 4 位十六进制 | `#F00F` |
| `#RRGGBB` | 6 位十六进制 | `#FF0000` |
| `#RRGGBBAA` | 8 位十六进制 | `#FF0000FF` |
| `rgb(r, g, b)` | RGB 函数 | `rgb(255, 0, 0)` |
| `rgba(r, g, b, a)` | RGBA 函数 | `rgba(255, 0, 0, 0.5)` |
| 命名颜色 | 预定义颜色名 | `red`, `blue`, `transparent` |
| `var(--name)` | 主题变量引用 | `var(--primary)` |

### 内联 style 属性

除 `<style>` 块外，元素还支持 Tailwind 风格的内联 `style` 属性：

```vue
<template>
  <VBox style="bg-white p-4 rounded shadow">
    <Text style="text-2xl font-bold">标题</Text>
    <Button style="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded">
      按钮
    </Button>
  </VBox>
</template>
```

内联 `style` 属性的优先级高于 `<style>` 块中的规则。动态绑定使用 `:style` 语法：

```vue
<template>
  <VBox :style="isActive ? 'bg-blue-500 text-white' : 'bg-gray-200'">
    内容
  </VBox>
</template>
```

## 数据绑定规范

GGWidget 数据绑定系统提供声明式的响应式数据流，基于 `Observable`/`Computed`/`DataBinding` 运行时基础设施。

### 响应式状态

#### `ref` — 可变响应式引用

在 `<script setup>` 中使用 `ref` 创建响应式状态：

```vue
<script setup>
let count: f64 = 0
let name: string = "Alice"
let items: Array<string> = []
let isVisible: bool = true
</script>
```

> GGWidget 中 `<script setup>` 块的顶层变量声明自动成为响应式状态（等价于 Vue 的 `ref`），变量变更时自动触发 UI 更新。

#### `computed` — 派生计算属性

计算属性根据依赖自动更新：

```vue
<script setup>
let firstName: string = "Alice"
let lastName: string = "Smith"

let fullName: string = $"{firstName} {lastName}"
let itemCount: i32 = items.length
</script>
```

> 计算属性通过表达式依赖追踪实现，当 `firstName` 或 `lastName` 变更时，`fullName` 自动重新计算。

### 绑定模式

GGWidget 支持三种绑定模式：

| 模式 | 语法 | 说明 |
| :--- | :--- | :--- |
| 单向绑定 | `:prop="expr"` | 源 → 目标，源变更时更新目标 |
| 双向绑定 | `::prop="expr"` | 源 ↔ 目标，任一变更同步另一 |
| 一次性绑定 | `:prop="expr"` (once) | 仅初始化时绑定一次 |

### 模板插值

使用 `{{ expression }}` 在文本中进行插值：

```vue
<template>
  <Text>当前计数：{{ count }}</Text>
  <Text>全名：{{ fullName }}</Text>
</template>
```

### 属性绑定

使用 `:attr` 前缀将表达式绑定到元素属性：

```vue
<template>
  <!-- 单向绑定 -->
  <Text :foreground="isActive ? green : gray">状态文本</Text>
  <Image :src="imageUrl" />
  <VBox :visibility="isVisible ? visible : collapsed">内容</VBox>

  <!-- 动态样式绑定 -->
  <Button :style="isActive ? 'bg-blue-500 text-white' : 'bg-gray-200'">
    按钮
  </Button>
</template>
```

### 双向绑定

使用 `::attr` 前缀实现双向绑定，常用于表单控件：

```vue
<script setup>
let username: string = ""
let volume: f64 = 50.0
let isEnabled: bool = true
</script>

<template>
  <!-- 文本输入双向绑定 -->
  <TextBox ::text="username" placeholder="请输入用户名" />

  <!-- 滑块双向绑定 -->
  <Slider ::value="volume" minimum="0" maximum="100" />

  <!-- 复选框双向绑定 -->
  <CheckBox ::is-checked="isEnabled" label="启用" />

  <!-- 显示绑定值 -->
  <Text>用户名：{{ username }}</Text>
  <Text>音量：{{ volume }}</Text>
</template>
```

双向绑定的语义：
- **源 → 目标**：当脚本变量变更时，更新控件属性
- **目标 → 源**：当控件属性变更时（用户输入），更新脚本变量

### 命令绑定

命令绑定将用户交互映射到脚本函数，使用 `@event` 前缀：

```vue
<script setup>
let count: f64 = 0

fn increment() {
  count = count + 1
}

fn decrement() {
  count = count - 1
}

fn reset() {
  count = 0
}
</script>

<template>
  <HBox spacing="8">
    <Button @click="decrement">-</Button>
    <Text>{{ count }}</Text>
    <Button @click="increment">+</Button>
    <Button @click="reset">重置</Button>
  </HBox>
</template>
```

命令绑定支持传递参数：

```vue
<script setup>
fn selectTab(index: i32) {
  currentTab = index
}
</script>

<template>
  <HBox>
    <Button @click="selectTab(0)">首页</Button>
    <Button @click="selectTab(1)">设置</Button>
    <Button @click="selectTab(2)">关于</Button>
  </HBox>
</template>
```

### ViewModel 模式

对于复杂组件，可使用 ViewModel 封装状态和逻辑：

```vue
<script setup>
let viewModel = ViewModel {
  count: Observable(0),
  step: Observable(1),
  computedCount: Computed(() => viewModel.count * 2)
}

fn increment() {
  viewModel.count = viewModel.count + viewModel.step
}
</script>

<template>
  <VBox>
    <Text>计数：{{ viewModel.count }}</Text>
    <Text>双倍：{{ viewModel.computedCount }}</Text>
    <Button @click="increment">+{{ viewModel.step }}</Button>
  </VBox>
</template>
```

## 事件系统规范

GGWidget 事件系统提供类型安全的事件处理，支持冒泡路由和直接路由两种策略。

### 事件绑定

使用 `@event` 前缀在模板中绑定事件处理器：

```vue
<template>
  <Button @click="handleClick">点击</Button>
  <TextBox @input="handleInput" @keydown="handleKey" />
  <ScrollView @wheel="handleWheel" />
</template>
```

### 事件类型

GGWidget 定义以下事件类型，对应运行时 `WidgetEventArgs` 体系：

| 事件名 | 事件参数类型 | 说明 |
| :--- | :--- | :--- |
| `@click` | `MouseEventArgs` | 鼠标点击 |
| `@mousedown` | `MouseEventArgs` | 鼠标按下 |
| `@mouseup` | `MouseEventArgs` | 鼠标释放 |
| `@mousemove` | `MouseEventArgs` | 鼠标移动 |
| `@mouseenter` | `MouseEventArgs` | 鼠标进入元素 |
| `@mouseleave` | `MouseEventArgs` | 鼠标离开元素 |
| `@wheel` | `WheelEventArgs` | 滚轮滚动 |
| `@keydown` | `KeyEventArgs` | 键盘按下 |
| `@keyup` | `KeyEventArgs` | 键盘释放 |
| `@input` | `TextInputEventArgs` | 文本输入 |
| `@focus` | `FocusEventArgs` | 获得焦点 |
| `@blur` | `FocusEventArgs` | 失去焦点 |

### 事件参数

#### MouseEventArgs

| 属性 | 类型 | 说明 |
| :--- | :--- | :--- |
| `x` | `f64` | 鼠标 X 坐标（相对元素） |
| `y` | `f64` | 鼠标 Y 坐标（相对元素） |
| `button` | `none` \| `left` \| `middle` \| `right` | 鼠标按键 |
| `click_count` | `i32` | 点击次数（双击=2） |
| `delta_x` | `f64` | X 方向移动量 |
| `delta_y` | `f64` | Y 方向移动量 |

#### KeyEventArgs

| 属性 | 类型 | 说明 |
| :--- | :--- | :--- |
| `key` | `Key` 枚举 | 按键标识 |
| `modifiers` | `none` \| `shift` \| `ctrl` \| `alt` | 修饰键 |
| `is_repeat` | `bool` | 是否重复按键 |

#### TextInputEventArgs

| 属性 | 类型 | 说明 |
| :--- | :--- | :--- |
| `text` | `string` | 输入的文本 |

#### WheelEventArgs

| 属性 | 类型 | 说明 |
| :--- | :--- | :--- |
| `x` | `f64` | 鼠标 X 坐标 |
| `y` | `f64` | 鼠标 Y 坐标 |
| `delta` | `f64` | 滚轮滚动量 |

#### FocusEventArgs

| 属性 | 类型 | 说明 |
| :--- | :--- | :--- |
| `old_focus` | `WidgetElement?` | 原焦点元素 |
| `new_focus` | `WidgetElement?` | 新焦点元素 |

### 事件路由

GGWidget 支持两种事件路由策略：

| 路由策略 | 说明 | 使用场景 |
| :--- | :--- | :--- |
| **冒泡路由** (Bubble) | 从目标元素向父元素逐级派发 | 大多数交互事件（click、mousedown 等） |
| **直接路由** (Direct) | 仅派发给目标元素 | 焦点事件、特定控件事件 |

冒泡路由可通过设置 `event.handled = true` 中断传播：

```vue
<script setup>
fn handleInnerClick(event: MouseEventArgs) {
  event.handled = true
}

fn handleOuterClick(event: MouseEventArgs) {
}
</script>

<template>
  <VBox @click="handleOuterClick">
    <Button @click="handleInnerClick">内部按钮</Button>
  </VBox>
</template>
```

### 焦点管理

GGWidget 提供内置的焦点管理系统：

```vue
<script setup>
fn handleTab(event: KeyEventArgs) {
  if (event.key == Key.Tab) {
    event.handled = true
  }
}
</script>

<template>
  <VBox>
    <TextBox :is-focusable="true" @keydown="handleTab" />
    <TextBox :is-focusable="true" />
    <Button :is-focusable="true">提交</Button>
  </VBox>
</template>
```

| 属性 | 类型 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- |
| `is-focusable` | `bool` | `false` | 是否可接收焦点 |
| `is-focused` | `bool` | `false` | 当前是否聚焦（只读） |
| `is-enabled` | `bool` | `true` | 是否启用（禁用时跳过焦点和事件） |

Tab 键导航按 DOM 顺序在可聚焦元素间移动焦点。

### 拖拽事件

GGWidget 通过鼠标事件组合实现拖拽交互：

```vue
<script setup>
let isDragging: bool = false
let dragOffsetX: f64 = 0
let dragOffsetY: f64 = 0
let posX: f64 = 100
let posY: f64 = 100

fn onDragStart(event: MouseEventArgs) {
  isDragging = true
  dragOffsetX = event.x
  dragOffsetY = event.y
}

fn onDragMove(event: MouseEventArgs) {
  if isDragging {
    posX = posX + event.delta_x
    posY = posY + event.delta_y
  }
}

fn onDragEnd(event: MouseEventArgs) {
  isDragging = false
}
</script>

<template>
  <Stack>
    <VBox
      :left="posX" :top="posY"
      @mousedown="onDragStart"
      @mousemove="onDragMove"
      @mouseup="onDragEnd"
    >
      <Text>可拖拽面板</Text>
    </VBox>
  </Stack>
</template>
```

## 下一步

- 阅读 [编辑器架构](../development/editor.md) 了解 Widget 在编辑器中的使用
- 阅读 [gg-shader 指南](gg-shader.md) 了解着色器编写
- 查看 [示例项目](../../examples/) 了解实际用法