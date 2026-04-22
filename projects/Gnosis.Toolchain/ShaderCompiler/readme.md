# 📖 Frontend 着色器编译器前端模块

## 📋 概述

Frontend 模块负责 gg-shader 源码的词法分析、语法解析和语义分析。

## 🏗️ 组件结构

```
Frontend/
├── GgShaderCompiler.cs       # 编译器主类
├── GgShaderParser.cs         # 语法解析器
└── ShaderSemanticAnalyzer.cs # 语义分析器
```

## 🔄 处理流程

```
gg-shader 源码
      ↓
┌─────────────────┐
│ GgShaderParser  │ 语法解析
└─────────────────┘
      ↓
┌─────────────────┐
│SemanticAnalyzer │ 语义分析
└─────────────────┘
      ↓
着色器 AST
```

## 🔗 相关模块

- [Backend](../Backend) - 后端代码生成
- [Shader](../../Shader) - 着色器定义
