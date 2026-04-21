# 🎮 GameUI 游戏 UI 模块

## 📋 概述

GameUI 模块提供游戏运行时 UI 系统，支持高性能渲染、Canvas 批处理、世界空间 UI 等功能。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| Canvas 管理 | 管理 UI Canvas 和渲染层级 |
| 批处理渲染 | 合并 Draw Call 优化性能 |
| 输入路由 | UI 输入事件分发 |
| 世界空间 UI | 支持 3D 世界中的 UI |

## 🏗️ 组件结构

```
GameUI/
├── ICanvas.cs             # Canvas 接口
├── Canvas.cs              # Canvas 实现
├── ICanvasRenderer.cs     # Canvas 渲染器接口
├── CanvasRenderer.cs      # Canvas 渲染器
├── IUIElement.cs          # UI 元素接口
├── UIElement.cs           # UI 元素
├── IStyleBox.cs           # 样式盒接口
├── StyleBox.cs            # 样式盒
├── IWidgetComponent.cs    # Widget 组件接口
├── GameWidgetComponent.cs # 游戏 Widget 组件
├── IUIRenderSystem.cs     # UI 渲染系统接口
├── UIRenderSystem.cs      # UI 渲染系统
├── IUIInputRouter.cs      # 输入路由接口
├── UIInputRouter.cs       # 输入路由
├── UIVertex.cs            # UI 顶点
├── AtlasUV.cs             # 图集 UV
├── UIRenderMode.cs        # 渲染模式
└── UIElementType.cs       # 元素类型
```

## 📊 渲染模式

| 模式 | 描述 |
|:---|:---|
| `ScreenSpace` | 屏幕空间 UI |
| `WorldSpace` | 世界空间 UI |

## 🔗 相关模块

- [Widget](../Widget) - 编辑器 Widget
- [RHI](../RHI) - 渲染接口
