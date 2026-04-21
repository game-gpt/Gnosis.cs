# 🖼️ Widget 编辑器 Widget 渲染模块

## 📋 概述

Widget 模块提供编辑器 UI 的渲染支持，包括布局引擎、组件渲染和事件处理。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 布局计算 | 计算组件布局和尺寸 |
| 组件渲染 | 渲染各类 UI 组件 |
| 事件处理 | 处理鼠标、键盘事件 |

## 🏗️ 组件结构

```
Widget/
├── IWidget.cs             # Widget 接口
├── Widget.cs              # Widget 基类
├── ILayoutEngine.cs       # 布局引擎接口
├── LayoutEngine.cs        # 布局引擎
├── IWidgetRenderer.cs     # 渲染器接口
├── IWidgetTreeRenderer.cs # 树渲染器接口
├── WidgetTreeRenderer.cs  # 树渲染器
├── UiRenderer.cs          # UI 渲染器
├── ButtonWidget.cs        # 按钮 Widget
├── TextWidget.cs          # 文本 Widget
├── RectWidget.cs          # 矩形 Widget
├── ContainerWidget.cs     # 容器 Widget
├── HBox.cs                # 水平布局
├── VBox.cs                # 垂直布局
├── Stack.cs               # 堆叠布局
├── Wrap.cs                # 换行布局
├── Expanded.cs            # 扩展填充
├── Flexible.cs            # 弹性布局
├── SizedBox.cs            # 固定尺寸盒
├── SeparatorWidget.cs     # 分隔符
├── ClipRect.cs            # 裁剪矩形
└── Size.cs, Rect.cs, ...  # 辅助类型
```

## 📐 布局系统

```
┌─────────────────────────────────────┐
│            Container                 │
│  ┌─────────────────────────────┐    │
│  │            HBox              │    │
│  │  ┌─────┐ ┌─────┐ ┌─────┐    │    │
│  │  │  A  │ │  B  │ │  C  │    │    │
│  │  └─────┘ └─────┘ └─────┘    │    │
│  └─────────────────────────────┘    │
│  ┌─────────────────────────────┐    │
│  │            VBox              │    │
│  │  ┌─────────────────────┐    │    │
│  │  │          D           │    │    │
│  │  └─────────────────────┘    │    │
│  │  ┌─────────────────────┐    │    │
│  │  │          E           │    │    │
│  │  └─────────────────────┘    │    │
│  └─────────────────────────────┘    │
└─────────────────────────────────────┘
```

## 🔗 相关模块

- [GameUI](../GameUI) - 游戏 UI
- [Editor/Widget](../../Editor/Widget) - 编辑器 Widget
