# 🌳 AST 抽象语法树模块

## 📋 概述

AST（Abstract Syntax Tree）模块定义了 gg-script 语言的抽象语法树节点，是编译器前端和后端之间的核心数据结构。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 节点定义 | 定义所有语法结构对应的 AST 节点 |
| 访问者模式 | 提供 `IAstVisitor` 接口支持树遍历 |
| 源码映射 | 每个节点记录源码位置，支持调试定位 |

## 🏗️ 节点类型

### 📝 表达式节点

| 节点 | 语法示例 | 描述 |
|:---|:---|:---|
| `BinaryExpr` | `a + b` | 二元运算表达式 |
| `UnaryExpr` | `-x`, `!flag` | 一元运算表达式 |
| `CallExpr` | `foo(a, b)` | 函数调用表达式 |
| `MemberAccessExpr` | `obj.field` | 成员访问表达式 |
| `IndexExpr` | `arr[i]` | 索引访问表达式 |
| `LiteralExpr` | `42`, `"hello"` | 字面量表达式 |
| `LambdaExpr` | `x => x * 2` | Lambda 表达式 |
| `AssignmentExpr` | `x = 10` | 赋值表达式 |
| `SwizzleExpr` | `vec.xyz` | 向量分量重组 |
| `QueryExpr` | `query all(A, B)` | ECS 查询表达式 |

### 📋 语句节点

| 节点 | 语法示例 | 描述 |
|:---|:---|:---|
| `BlockStmt` | `{ ... }` | 代码块 |
| `IfStatement` | `if cond { }` | 条件语句 |
| `ForStmt` | `for init; cond; update { }` | for 循环 |
| `LoopStmt` | `for item in items { }` | for-each 循环 |
| `WhileStmt` | `while cond { }` | while 循环 |
| `ReturnStatement` | `return value` | 返回语句 |
| `DiscardStmt` | `_ = foo()` | 丢弃语句 |

### 📦 声明节点

| 节点 | 语法示例 | 描述 |
|:---|:---|:---|
| `FunctionDecl` | `micro foo() { }` | 函数声明 |
| `VariableDecl` | `let x = 10` | 变量声明 |
| `StructDecl` | `struct Point { }` | 结构体声明 |
| `ComponentDecl` | `component Pos { }` | ECS 组件声明 |
| `SystemDecl` | `system Move { }` | ECS 系统声明 |
| `WidgetDecl` | `widget Button { }` | UI 组件声明 |
| `SceneDecl` | `scene Level { }` | 场景声明 |
| `PluginDecl` | `plugin Audio { }` | 插件声明 |

## 🔧 使用示例

```csharp
# 使用访问者模式遍历 AST
class Printer : IAstVisitor<string>
{
    public string VisitBinaryExpr(BinaryExpr node) =>
        $"({Visit(node.Left)} {node.Operator} {Visit(node.Right)})";
    
    public string VisitLiteralExpr(LiteralExpr node) =>
        node.Value?.ToString() ?? "null";
}
```

## 🔗 相关模块

- [Frontend](../Frontend) - 生成 AST
- [Backend](../Backend) - 消费 AST 生成代码
