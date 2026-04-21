# 🎨 Rendering 渲染模块

## 📋 概述

Rendering 模块是 gg 引擎的图形渲染核心，提供跨平台的渲染硬件抽象层（RHI）、渲染管线、着色器编译和 UI 渲染等功能。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| RHI 抽象 | 跨平台渲染硬件接口抽象 |
| 渲染管线 | 可配置的渲染管线架构 |
| 着色器编译 | gg-shader 到 SPIR-V 编译 |
| UI 渲染 | 编辑器 Widget 和游戏 UI 渲染 |

## 🏗️ 模块结构

```
Rendering/
├── RHI/                # 渲染硬件接口
├── Pipeline/           # 渲染管线
├── Shader/             # 着色器定义
├── ShaderCompiler/     # 着色器编译器
├── ShaderGenerator/    # 着色器源码生成器
├── Renderer/           # 渲染器实现
├── GameUI/             # 游戏 UI 系统
└── Widget/             # 编辑器 Widget 渲染
```

## 🔄 渲染流程

```
场景数据
    ↓
┌─────────────┐
│ RenderGraph │ 帧图调度
└─────────────┘
    ↓
┌─────────────┐
│  Pipeline   │ 渲染管线
└─────────────┘
    ↓
┌─────────────┐
│    RHI      │ 硬件抽象
└─────────────┘
    ↓
┌─────────────┐
│ GPU Driver  │ 驱动执行
└─────────────┘
```

## 🔗 相关模块

- [Compiler](../Compiler) - 着色器编译
- [Assets](../Assets) - 渲染资产加载
