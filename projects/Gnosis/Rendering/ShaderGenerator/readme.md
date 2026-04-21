# ⚡ ShaderGenerator 着色器源码生成器模块

## 📋 概述

ShaderGenerator 模块是一个 C# 源码生成器，在编译时自动生成着色器相关的 C# 代码。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 源码生成 | 编译时生成着色器包装代码 |
| 类型映射 | 自动生成着色器参数类型 |

## 🏗️ 组件结构

```
ShaderGenerator/
├── Gnosis.ShaderGenerator.csproj      # 项目文件
└── GnosisShaderSourceGenerator.cs     # 源码生成器
```

## 🔗 相关模块

- [Shader](../Shader) - 着色器定义
- [ShaderCompiler](../ShaderCompiler) - 着色器编译
