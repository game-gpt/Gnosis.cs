# 贡献指南

感谢你对 Gnosis 引擎的关注！本文档介绍如何参与项目开发。

---

## 开发环境

### 必需组件

| 组件 | 版本要求 | 说明 |
|------|----------|------|
| .NET SDK | 8.0+ | C# 编译与构建 |
| Git | 最新版 | 版本控制 |

### 推荐工具

| 工具 | 用途 |
|------|------|
| Visual Studio / Rider | C# 开发与调试 |
| VS Code | gg 语言编辑 |

---

## 获取源码

```bash
# Fork 仓库后
git clone https://github.com/your-username/Gnosis.cs.git
cd Gnosis.cs

# 添加上游仓库
git remote add upstream https://github.com/original-org/Gnosis.cs.git
```

---

## 开发流程

### 1. 创建分支

```bash
# 从 main 创建特性分支
git checkout main
git pull upstream main
git checkout -b feature/your-feature-name
```

### 2. 开发与测试

```bash
# 构建
dotnet build

# 运行测试
dotnet test

# 运行特定测试
dotnet test --filter "FullyQualifiedName~YourTestName"
```

### 3. 提交代码

遵循 [Git Emoji 规范](#git-emoji-规范)：

```bash
git add .
git commit -m "✨ 添加传奇系统

- 新增传奇系统接口 `ILegendSystem`
- 新增传奇系统实现 `LegendSystem`
- 新增传奇系统测试用例 `Gnosis.Tests.LegendSystem`"
```

### 4. 推送与创建 PR

```bash
git push origin feature/your-feature-name
```

然后在 GitHub 上创建 Pull Request。

---

## Git Emoji 规范

### 提交格式

```
{Emoji} {简短总结}

- {详细变更条目 1}
- {详细变更条目 2}
```

中英文之间要有空格，技术术语要用反引号引起来。

### 常用 Emoji

| Emoji | 含义 |
|:---|:---|
| ✨ | 引入新功能 |
| 🐛 | 修复 bug |
| 📝 | 更新文档 |
| 🎨 | 改进代码格式/结构 |
| ♻️ | 重构代码 |
| ⚡️ | 提升性能 |
| 🔧 | 更新配置 |
| 🚚 | 移动或重命名文件 |
| 🗑️ | 删除文件 |
| 🔒️ | 修复安全问题 |
| 🚀 | 部署 |
| 🧪 | 添加/更新测试 |
| 📦️ | 更新依赖 |
| 🚧 | 进行中工作 |
| 🎉 | 初始提交 |
| 👥 | 添加贡献者 |
| 📄 | 添加/更新许可证 |
| 🔖 | 发布/版本标签 |

### 提交示例

```
✨ 添加传奇系统
- 新增传奇系统接口 `ILegendSystem`
- 新增传奇系统实现 `LegendSystem`
- 新增传奇系统测试用例 `Gnosis.Tests.LegendSystem`
```

---

## 代码规范

### C# 代码

遵循 [C# 编码规范](./coding-standards.md)，核心要点：

- 注释使用中文
- 大括号独占一行（Allman 风格）
- 大文件必须使用 `#region` 划分
- 命名空间遵循 `Gnosis.{包名}.{子模块}` 模式
- 禁止使用 `System.Reflection`（违反多阶段编程原则）

### 包依赖方向

依赖方向必须遵循：**基础层 ← 核心层 ← 子系统层**，绝不允许反向依赖。

### 扩展点模式

第三方接入使用标准后缀：

| 后缀 | 使用场景 |
|:---|:---|
| `Adapter` | 第三方数据格式或运行时接入 |
| `Provider` | 数据存储或配置后端 |
| `Driver` | 硬件或底层库抽象 |

---

## PR 检查清单

提交 PR 前请确认：

- [ ] 代码通过 `dotnet build` 编译
- [ ] 测试通过 `dotnet test`
- [ ] 遵循 [C# 编码规范](./coding-standards.md)
- [ ] 依赖方向正确（基础层 ← 核心层 ← 子系统层）
- [ ] 新增公共 API 有 XML 文档注释（中文）
- [ ] 新增功能有对应测试
- [ ] 提交消息遵循 Git Emoji 规范
- [ ] 不包含密钥、凭据等敏感信息

---

## 架构理解

在贡献代码前，请务必理解 [三层蛋糕模型](./architecture.md#一三层蛋糕模型必须牢记)：

| 层次 | 语言 | 你应该写的代码 |
|:---|:---|:---|
| Layer 1: 元引擎层 | C# | 引擎基础设施（编译器、VM、RHI、ECS 运行时） |
| Layer 2: 游戏引擎层 | C# | 编辑器、资产管线、构建工具 |
| Layer 3: 游戏内容层 | GG 语言族 | 游戏逻辑、着色器、编辑器 Widget |

**致命错误**：
- ❌ 在 `Gnosis.Core` 中写游戏逻辑
- ❌ 在 `gg-script` 中实现渲染管线
- ❌ 在引擎包中直接依赖第三方闭源 SDK（应使用 Adapter/Provider/Driver 接口）

---

## 问题反馈

- **Bug 报告**：使用 GitHub Issues，附上复现步骤和环境信息
- **功能请求**：使用 GitHub Issues，描述使用场景与预期行为
- **安全漏洞**：请勿公开报告，通过安全渠道联系维护者
