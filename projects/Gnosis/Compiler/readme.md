# 🔨 Compiler 编译器模块

## 📋 概述

Compiler 模块是 gg 引擎的核心编译基础设施，负责将 gg-script、gg-shader、gg-widget 等语言编译为可执行的字节码或原生代码。采用多阶段编程（MSP）架构，支持元编程和编译时计算。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 词法分析 | 将源代码转换为 Token 流 |
| 语法解析 | 构建抽象语法树（AST） |
| 语义分析 | 类型检查、作用域解析 |
| 元编程展开 | 编译时宏展开和代码生成 |
| 字节码生成 | 生成 gg 虚拟机可执行的字节码 |
| 原生代码生成 | 生成平台特定的原生代码 |

## 🏗️ 模块结构

```
Compiler/
├── Frontend/         # 前端：词法分析、语法解析、语义分析
├── AST/              # 抽象语法树节点定义
├── Backend/          # 后端：字节码生成、原生代码生成
├── Cache/            # 编译缓存，支持增量编译
└── Diagnostics/      # 编译诊断信息（错误、警告）
```

## 🔄 编译流程

```
源代码 (.gg)
    ↓
┌─────────────┐
│   Lexer     │ 词法分析
└─────────────┘
    ↓
┌─────────────┐
│   Parser    │ 语法解析
└─────────────┘
    ↓
┌─────────────┐
│    AST      │ 抽象语法树
└─────────────┘
    ↓
┌─────────────┐
│ Semantic    │ 语义分析
│ Analyzer    │
└─────────────┘
    ↓
┌─────────────┐
│   Meta      │ 元编程展开
│ Evaluator   │
└─────────────┘
    ↓
┌─────────────┐
│  Backend    │ 代码生成
└─────────────┘
    ↓
字节码 (.ggc) / 原生代码
```

## 🌐 多目标编译

| 目标平台 | 输出格式 | 描述 |
|:---|:---|:---|
| Windows x64 | Native DLL | 原生动态链接库 |
| Linux x64 | Native SO | 原生共享库 |
| Android ARM | Native SO | 移动端原生库 |
| iOS ARM | Native Dylib | iOS 原生库 |
| WebAssembly | WASM | 浏览器运行 |
| GG VM | Bytecode | 跨平台字节码 |

## 🔗 相关模块

- [Interpreter](../Interpreter) - 字节码执行
- [Rendering/ShaderCompiler](../Rendering/ShaderCompiler) - 着色器编译
- [Editor/WidgetCompiler](../Editor/WidgetCompiler) - Widget 编译
