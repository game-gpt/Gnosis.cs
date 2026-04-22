# 开发指南

本目录包含 Gnosis 引擎的开发指南文档，面向引擎开发者与游戏开发者。

---

## 文档索引

### 核心文档

| 文档 | 描述 |
|------|------|
| [开发入门](./getting-started.md) | 开发环境配置与工作流程 |
| [架构设计](./architecture.md) | 引擎整体架构与多阶段编程模型 |

### 子系统文档

| 文档 | 对应包 | 描述 |
|------|--------|------|
| [渲染系统](./rendering.md) | `Gnosis.Graphic` | RHI 抽象层、渲染管线与管线优化 |
| [网络架构](./network.md) | `Gnosis.Network` | 帧同步与状态同步的融合 |
| [编辑器架构](./editor.md) | `Gnosis.Widget` | 编辑器架构与 Widget 系统 |
| [热更新与热重载](./hot-update.md) | `Gnosis.Runtime` | 热重载与热更新机制 |
| [反作弊系统](./anti-cheat.md) | `Gnosis.Security` | 分层防御与安全加固 |
| [配置表系统](./config-tables.md) | `Gnosis.Asset` | 配置表格式规范与使用方式 |

---

## 语言指南

GG 语言族的详细语法与使用说明请参阅 [languages/](../languages/) 目录：

| 语言 | 文档 | 对应包 |
|------|------|--------|
| gg-script | [gg-script.md](../languages/gg-script.md) | `Gnosis.Runtime` |
| gg-shader | [gg-shader.md](../languages/gg-shader.md) | `Gnosis.Graphic.Shader` |
| gg-widget | [gg-widget.md](../languages/gg-widget.md) | `Gnosis.Widget` |
| gg-object | [gg-object.md](../languages/gg-object.md) | `Gnosis.Asset.Format` |
| gg-neural | [gg-neural.md](../languages/gg-neural.md) | `Gnosis.Neural` |

---

## 架构参考

完整的 25 包结构与三层蛋糕模型请参阅 [架构详解](../maintenance/architecture.md)。
