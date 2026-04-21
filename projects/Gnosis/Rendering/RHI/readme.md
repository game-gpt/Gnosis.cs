# 🔌 RHI 渲染硬件接口模块

## 📋 概述

RHI（Render Hardware Interface）模块提供跨平台的图形 API 抽象层，支持 Vulkan、DirectX 12、Metal 和 WebGL 等后端。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 设备管理 | GPU 设备创建和资源管理 |
| 命令提交 | 渲染命令录制和提交 |
| 资源管理 | 缓冲区、纹理、着色器资源 |
| 管线状态 | PSO 创建和管理 |

## 🏗️ 组件结构

```
RHI/
├── IDevice.cs           # 设备接口
├── ICommandTable.cs     # 命令表接口
├── IResource.cs         # 资源接口
├── IPipelineState.cs    # 管线状态接口
├── IMaterial.cs         # 材质接口
├── IMeshFilter.cs       # 网格过滤器接口
├── ICamera.cs           # 相机接口
├── IView.cs             # 视图接口
├── ITransform.cs        # 变换接口
├── BlendMode.cs         # 混合模式
├── CompareFunction.cs   # 深度比较函数
├── CullMode.cs          # 剔除模式
├── ResourceFormat.cs    # 资源格式
├── ResourceType.cs      # 资源类型
└── RenderPathFlag.cs    # 渲染路径标志
```

## 📊 支持的后端

| 后端 | 平台 | 描述 |
|:---|:---|:---|
| Vulkan | Windows/Linux/Android | 跨平台高性能 |
| DirectX 12 | Windows | Windows 原生 |
| Metal | macOS/iOS | Apple 平台 |
| WebGL | Browser | Web 平台 |

## 🔗 相关模块

- [Pipeline](../Pipeline) - 渲染管线
- [Shader](../Shader) - 着色器
