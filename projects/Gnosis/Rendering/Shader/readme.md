# 🎭 Shader 着色器模块

## 📋 概述

Shader 模块定义着色器相关的类型和接口，支持传统光栅化、光线追踪、神经渲染等多种渲染范式。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 着色器定义 | 定义着色器资源和参数 |
| 多后端支持 | 支持 SPIR-V、NeRF、Diffusion 等后端 |
| 参数管理 | 管理着色器 Uniform 和采样器 |

## 🏗️ 组件结构

```
Shader/
├── IShaderCompiler.cs       # 着色器编译器接口
├── IShaderBackend.cs        # 着色器后端接口
├── IShaderModule.cs         # 着色器模块接口
├── IMicroFunction.cs        # Micro 函数接口
├── Shader.cs                # 着色器主类
├── ShaderData.cs            # 着色器数据
├── ShaderAttribute.cs       # 着色器属性
├── ShaderParameter.cs       # 着色器参数
├── ShaderUniform.cs         # Uniform 定义
├── ShaderSampler.cs         # 采样器定义
├── ShaderStage.cs           # 着色器阶段
├── ShaderTarget.cs          # 着色器目标
├── ShaderLanguage.cs        # 着色器语言
├── MicroFunctionKind.cs     # Micro 函数类型
├── NeuralShaderBackend.cs   # 神经渲染后端
└── DiffusionShaderBackend.cs # 扩散模型后端
```

## 📊 着色器阶段

| 阶段 | 装饰器 | 描述 |
|:---|:---|:---|
| Vertex | `[Vertex]` | 顶点着色器 |
| Fragment | `[Fragment]` | 片段着色器 |
| Compute | `[Compute]` | 计算着色器 |
| RayGen | `[RayGen]` | 光线生成 |
| ClosestHit | `[ClosestHit]` | 最近命中 |
| Miss | `[Miss]` | 未命中 |

## 🔗 相关模块

- [ShaderCompiler](../ShaderCompiler) - 着色器编译
- [RHI](../RHI) - 硬件接口
