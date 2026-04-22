# ⚙️ Backend 后端代码生成模块

## 📋 概述

Backend 模块负责将 AST 转换为可执行代码，支持字节码生成和原生代码生成两种模式。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 字节码生成 | 生成 GG 虚拟机可执行的字节码 |
| 原生代码生成 | 生成平台特定的原生机器码 |
| 优化 | 死代码消除、常量折叠等优化 |

## 🏗️ 组件结构

```
Backend/
├── BytecodeGenerator.cs    # 字节码生成器
└── NativeCodeGenerator.cs  # 原生代码生成器
```

## 🔄 生成流程

```
AST
  ↓
┌──────────────────┐
│ BytecodeGenerator │ → .ggc 字节码模块
└──────────────────┘
  ↓
┌──────────────────┐
│NativeCodeGenerator│ → .dll / .so / .dylib
└──────────────────┘
```

## 🎯 目标架构

| 架构 | 输出 | 描述 |
|:---|:---|:---|
| x86_64 | Native Code | 64 位 Intel/AMD |
| ARM64 | Native Code | 64 位 ARM |
| WASM | WebAssembly | 浏览器运行 |
| GGVM | Bytecode | 跨平台虚拟机 |

## 🔗 相关模块

- [AST](../AST) - 输入的语法树
- [Interpreter](../../Interpreter) - 执行字节码
