# SpirvCross.Net

**纯 C# SPIR-V 交叉编译器** — 零原生依赖，跨平台，AOT 友好。

## ✨ 特性

- **纯 C# 实现**：零 C/C++ 依赖，无需分发原生二进制
- **跨平台**：Windows / Linux / macOS / iOS / Android / WebAssembly
- **AOT 友好**：完全兼容 .NET Native AOT
- **多目标格式**：SPIR-V → HLSL / GLSL / MSL / WGSL / DXIL
- **可调试**：纯 C# 代码，可在 IDE 中单步跟踪
- **可扩展**：继承/覆写即可自定义翻译行为
- **生态友好**：复用 Acorn.SpirV 二进制编解码，与 .NET 游戏引擎生态无缝集成

## 📦 包

| 包 | 说明 | NuGet |
|---|---|---|
| `SpirvCross.Net` | 核心包：SPIR-V → HLSL/GLSL/MSL/WGSL 文本生成 | - |
| `SpirvCross.Net.Dxil` | DXIL 后端：SPIR-V → DXIL 二进制（依赖 Acorn.Dxil） | - |

## 🚀 快速开始

```csharp
using SpirvCross.Net;
using SpirvCross.Net.Target;

var translator = new SpirvTranslator(spirvBytecode);

var hlsl = translator.Translate(new HlslTarget());
var glsl = translator.Translate(new GlslTarget());
var msl = translator.Translate(new MslTarget());
var wgsl = translator.Translate(new WgslTarget());
```

## 🏗️ 架构

```
SPIR-V 字节码
    ↓ Acorn.SpirV 解码
SpirvModule（语义模型）
    ↓ SpirvTranslator 翻译
    ├── HlslTarget → HLSL 文本
    ├── GlslTarget → GLSL 文本
    ├── MslTarget  → MSL 文本
    └── WgslTarget → WGSL 文本
```

### 模块划分

| 目录 | 职责 |
|---|---|
| `Model/` | SPIR-V 语义模型（类型、函数、变量、装饰） |
| `Translate/` | 翻译器核心（类型映射、指令映射） |
| `Target/` | 目标格式生成器（HLSL/GLSL/MSL/WGSL） |
| `Optimize/` | SPIR-V 优化 Pass（DCE、常量折叠） |

## 📚 参考

- [SPIR-V Specification](https://registry.khronos.org/SPIR-V/specs/unified1/SPIRV.html)
- [naga](https://github.com/gfx-rs/naga) — Rust SPIR-V 交叉编译器（架构参考）
- [SPIRV-Cross](https://github.com/KhronosGroup/SPIRV-Cross) — C++ SPIR-V 交叉编译器（功能参考）

## 📄 许可证

MPL-2.0
