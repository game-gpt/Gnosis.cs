# 🔮 Spirv SPIR-V 生成模块

## 📋 概述

Spirv 模块负责将着色器 IR 转换为 SPIR-V 字节码，支持 Vulkan、OpenCL 等平台。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 字节码生成 | 生成标准 SPIR-V 字节码 |
| 类型缓存 | 缓存 SPIR-V 类型定义 |
| 优化 | SPIR-V 优化 |
| 验证 | SPIR-V 验证 |
| 反汇编 | SPIR-V 反汇编输出 |

## 🏗️ 组件结构

```
Spirv/
├── SpirvGenerator.cs    # SPIR-V 生成器
├── SpirvBuilder.cs      # SPIR-V 构建器
├── SpirvConstants.cs    # SPIR-V 常量
├── SpirvTypeCache.cs    # 类型缓存
├── SpirvOptimizer.cs    # 优化器
├── SpirvValidator.cs    # 验证器
└── SpirvDisassembler.cs # 反汇编器
```

## 🔗 相关模块

- [ShaderIR](../ShaderIR) - 输入 IR
- [RHI](../../../RHI) - 执行 SPIR-V
