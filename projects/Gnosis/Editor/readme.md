# ✏️ Editor 编辑器模块

## 📋 概述

Editor 模块提供 gg 引擎的可视化编辑器，支持场景编辑、资产浏览、调试等开发工具。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 编辑器窗口 | 管理编辑器窗口和面板 |
| Widget 系统 | 编辑器 UI 组件 |
| Widget 编译 | 编译 gg-widget 到编辑器组件 |

## 🏗️ 模块结构

```
Editor/
├── Widget/              # 编辑器 Widget 组件
├── WidgetCompiler/      # Widget 编译器
└── EditorWindow.cs      # 编辑器窗口基类
```

## 🔗 相关模块

- [Rendering/Widget](../Rendering/Widget) - Widget 渲染
- [Compiler](../Compiler) - 编译基础设施
