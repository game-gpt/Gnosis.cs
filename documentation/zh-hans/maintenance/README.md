# 维护指南

本目录包含 Gnosis 引擎的维护指南文档，面向引擎核心开发者。

---

## 文档索引

| 文档 | 描述 |
|------|------|
| [架构详解](./architecture.md) | 25 包结构与三层蛋糕模型（核心文档） |
| [编码规范](./coding-standards.md) | C# 代码风格指南 |
| [测试指南](./testing-guide.md) | 单元测试与集成测试 |
| [贡献指南](./contributing.md) | 如何参与项目开发 |
| [NoSQL 存储引擎](./nosql-database.md) | Gnosis.Database 嵌入式数据库设计文档 |

---

## 核心原则

在维护 Gnosis 引擎时，请始终牢记：

1. **三层蛋糕模型**：Layer 1（元引擎 C#）← Layer 2（游戏引擎 C#）← Layer 3（游戏内容 GG 语言族）
2. **依赖方向**：基础层 ← 核心层 ← 子系统层，绝不允许反向依赖
3. **扩展点模式**：第三方接入使用 `Adapter` / `Provider` / `Driver` 后缀
4. **命名规范**：包名单数名词，子模块不用 `Core`，避免 `-ing` 形式

详见 [架构详解](./architecture.md)。
