# 🔨 ShaderCompiler 着色器编译模块

## 📋 概述

ShaderCompiler 模块负责将 gg-shader 语言编译为 GPU 可执行的字节码，支持 SPIR-V 输出和多种渲染后端。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 前端解析 | 解析 gg-shader 源码 |
| IR 生成 | 生成着色器中间表示 |
| SPIR-V 生成 | 生成 SPIR-V 字节码 |
| 优化 | 着色器优化和验证 |

## 🏗️ 模块结构

```
ShaderCompiler/
├── Frontend/               # 前端
│   ├── GgShaderCompiler.cs # 编译器主类
│   ├── GgShaderParser.cs   # 语法解析
│   └── ShaderSemanticAnalyzer.cs # 语义分析
└── Backend/                # 后端
    ├── IrGenerator.cs      # IR 生成器
    ├── ShaderIR/           # 着色器 IR
    └── Spirv/              # SPIR-V 生成
```

## 🔄 编译流程

```
gg-shader 源码 (.ggs)
        ↓
┌─────────────────┐
│ GgShaderParser  │ 语法解析
└─────────────────┘
        ↓
┌─────────────────┐
│ SemanticAnalyzer│ 语义分析
└─────────────────┘
        ↓
┌─────────────────┐
│  IrGenerator    │ IR 生成
└─────────────────┘
        ↓
┌─────────────────┐
│  SpirvGenerator │ SPIR-V 生成
└─────────────────┘
        ↓
SPIR-V 字节码
```

## 🔗 相关模块

- [Shader](../Shader) - 着色器定义
- [Compiler](../../Compiler) - 编译基础设施
