# 🔧 WidgetCompiler Widget 编译模块

## 📋 概述

WidgetCompiler 模块负责将 gg-widget 语言编译为编辑器可用的组件。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| Widget 解析 | 解析 gg-widget 源文件 |
| 组件生成 | 生成编辑器组件代码 |
| 热重载 | 支持 Widget 热重载 |

## 🏗️ 组件结构

```
WidgetCompiler/
└── GgWidgetCompiler.cs    # Widget 编译器主类
```

## 🔗 相关模块

- [Editor](../) - 编辑器主模块
- [Compiler](../../Compiler) - 编译基础设施
