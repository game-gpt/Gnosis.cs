# 📖 Frontend 前端模块

## 📋 概述

Frontend 模块负责编译器的前端工作：词法分析、语法解析、语义分析和元编程展开。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 词法分析 | 将源代码转换为 Token 流 |
| 语法解析 | 构建 AST 抽象语法树 |
| 语义分析 | 类型检查、作用域解析 |
| 元编程展开 | 编译时宏展开和代码生成 |

## 🏗️ 组件结构

```
Frontend/
├── GgScriptLexer.cs           # 词法分析器
├── GgScriptParser.cs          # 语法解析器
├── SemanticAnalyzer.cs        # 语义分析器
├── BaseSemanticAnalyzer.cs    # 语义分析基类
└── MetaLanguageEvaluator.cs   # 元语言求值器
```

## 🔄 处理流程

```
源代码字符串
      ↓
┌─────────────┐
│   Lexer     │ → Token 流
└─────────────┘
      ↓
┌─────────────┐
│   Parser    │ → AST
└─────────────┘
      ↓
┌─────────────┐
│  Semantic   │ → 类型检查后的 AST
│  Analyzer   │
└─────────────┘
      ↓
┌─────────────┐
│   Meta      │ → 展开元编程后的 AST
│  Evaluator  │
└─────────────┘
```

## 🔗 相关模块

- [AST](../AST) - 语法树定义
- [Diagnostics](../Diagnostics) - 错误报告
- [Backend](../Backend) - 后端代码生成
