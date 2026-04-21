# 🖥️ Interpreter 解释器模块

## 📋 概述

Interpreter 模块是 gg 虚拟机的核心，负责执行编译器生成的字节码，支持热重载和模块化加载。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 字节码执行 | 解释执行 .ggc 字节码模块 |
| 内存管理 | 管理虚拟机内存空间 |
| 模块加载 | 动态加载和卸载字节码模块 |
| 热重载 | 支持运行时代码更新 |
| 原生函数 | 注册和调用原生 C# 函数 |

## 🏗️ 模块结构

```
Interpreter/
├── IR/                  # 中间表示
│   ├── BytecodeIR.cs    # 字节码 IR
│   └── OpCode.cs        # 操作码定义
└── VM/                  # 虚拟机
    ├── VMInterpreter.cs       # 虚拟机解释器
    ├── VMState.cs             # 虚拟机状态
    ├── VMStack.cs             # 虚拟机栈
    ├── MemoryManager.cs       # 内存管理器
    ├── NativeFunctionRegistry.cs # 原生函数注册表
    ├── BytecodeModuleAdapter.cs   # 模块适配器
    └── VMExceptions.cs        # 虚拟机异常
```

## 🔄 执行流程

```
字节码模块 (.ggc)
       ↓
┌─────────────────┐
│ ModuleAdapter   │ 加载模块
└─────────────────┘
       ↓
┌─────────────────┐
│  VMInterpreter  │ 解释执行
└─────────────────┘
       ↓
┌─────────────────┐
│  NativeFunc     │ 调用原生函数
│  Registry       │
└─────────────────┘
```

## 📊 操作码类型

| 类别 | 示例 | 描述 |
|:---|:---|:---|
| 加载 | `LoadConst`, `LoadLocal` | 加载数据到栈 |
| 存储 | `StoreLocal`, `StoreGlobal` | 存储数据 |
| 运算 | `Add`, `Sub`, `Mul` | 算术运算 |
| 控制 | `Jump`, `Call`, `Return` | 控制流 |
| 对象 | `New`, `GetField`, `SetField` | 对象操作 |

## 🔗 相关模块

- [Compiler](../Compiler) - 生成字节码
- [Core](../Core) - 核心类型
