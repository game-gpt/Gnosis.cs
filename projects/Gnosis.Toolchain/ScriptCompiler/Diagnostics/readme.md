# 🔍 Diagnostics 诊断模块

## 📋 概述

Diagnostics 模块负责收集和报告编译过程中的错误、警告和信息，为开发者提供清晰的反馈。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 错误收集 | 收集编译过程中的各类诊断信息 |
| 严重程度 | 区分错误、警告、信息等级别 |
| 源码定位 | 精确标记问题所在的源码位置 |

## 🏗️ 组件结构

```
Diagnostics/
├── Diagnostic.cs           # 诊断信息实体
├── DiagnosticSeverity.cs   # 严重程度枚举
└── DiagnosticSink.cs       # 诊断信息接收器
```

## 📊 严重程度

| 级别 | 图标 | 描述 |
|:---|:---|:---|
| Error | ❌ | 编译错误，阻止继续编译 |
| Warning | ⚠️ | 警告，可能存在问题 |
| Info | ℹ️ | 信息性提示 |
| Hint | 💡 | 优化建议 |

## 🔧 使用示例

```csharp
var sink = new DiagnosticSink();
sink.Error(SourceSpan, "未找到变量 'x'");
sink.Warning(SourceSpan, "未使用的变量 'y'");
```

## 🔗 相关模块

- [Frontend](../Frontend) - 生成诊断信息
- [Editor](../../Editor) - 显示诊断信息
