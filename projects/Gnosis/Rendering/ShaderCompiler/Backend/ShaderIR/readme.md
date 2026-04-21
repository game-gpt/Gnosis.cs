# 📝 ShaderIR 着色器中间表示模块

## 📋 概述

ShaderIR 模块定义着色器的中间表示，是前端解析和后端代码生成之间的桥梁。

## 🏗️ 组件结构

```
ShaderIR/
├── ShaderModuleIr.cs      # 着色器模块 IR
├── ShaderFunctionIr.cs    # 函数 IR
├── ShaderStructIr.cs      # 结构体 IR
├── ShaderResourceIr.cs    # 资源 IR
├── ShaderIrInstruction.cs # IR 指令
├── ShaderIrType.cs        # IR 类型
└── ExternalFunctionRef.cs  # 外部函数引用
```

## 🔗 相关模块

- [Spirv](../Spirv) - SPIR-V 生成
- [Frontend](../../Frontend) - 前端解析
