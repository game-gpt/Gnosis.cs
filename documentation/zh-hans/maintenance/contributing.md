# 贡献流程

本文档介绍如何为 Gnosis 引擎项目做出贡献。

## 行为准则

- 尊重所有贡献者
- 保持建设性的讨论
- 接受建设性的批评
- 关注对社区最有利的事情

## 贡献方式

### 报告问题

1. 搜索现有 Issues，确认问题未被报告
2. 使用 Issue 模板创建新 Issue
3. 提供详细的问题描述：
   - 复现步骤
   - 期望行为
   - 实际行为
   - 环境信息（操作系统、Rust 版本等）

### 提交代码

1. Fork 仓库
2. 创建功能分支
3. 编写代码和测试
4. 提交 Pull Request

## 开发流程

### 1. 设置开发环境

```bash
# Fork 后克隆仓库
git clone https://github.com/YOUR_USERNAME/Gnosis.cs.git
cd Gnosis.cs

# 添加上游仓库
git remote add upstream https://github.com/ORIGINAL_ORG/Gnosis.cs.git

# 安装依赖
cargo build
```

### 2. 创建分支

```bash
# 同步上游更改
git fetch upstream
git checkout main
git merge upstream/main

# 创建功能分支
git checkout -b feature/your-feature-name
```

### 3. 编写代码

遵循 [编码规范](coding-standards.md)：

- 使用 `cargo fmt` 格式化代码
- 使用 `cargo clippy` 检查代码质量
- 编写单元测试和集成测试

### 4. 提交更改

使用规范的提交信息：

```
<type>: <subject>

<body>

<footer>
```

#### 提交类型

| 类型 | 描述 |
| :--- | :--- |
| `feat` | 新功能 |
| `fix` | Bug 修复 |
| `docs` | 文档更新 |
| `style` | 代码格式（不影响功能） |
| `refactor` | 代码重构 |
| `test` | 测试相关 |
| `chore` | 构建/工具相关 |

#### 示例

```
feat(stg): 添加螺旋弹幕模式

- 实现 SpiralBulletPattern 类型
- 添加 angular_speed 和 radial_speed 参数
- 添加单元测试和集成测试

Closes #123
```

### 5. 推送更改

```bash
git push origin feature/your-feature-name
```

### 6. 创建 Pull Request

1. 访问 GitHub 仓库页面
2. 点击 "New Pull Request"
3. 选择你的分支
4. 填写 PR 模板：
   - 描述更改内容
   - 关联相关 Issue
   - 说明测试方法

## PR 检查清单

在提交 PR 前，请确认：

- [ ] 代码通过 `cargo fmt` 格式化
- [ ] 代码通过 `cargo clippy` 检查
- [ ] 所有测试通过 `cargo test`
- [ ] 新功能有对应的测试
- [ ] 文档已更新（如适用）
- [ ] 提交信息符合规范

## 代码审查

### 审查流程

1. 维护者会审查你的 PR
2. 可能会提出修改建议
3. 根据反馈进行修改
4. 审查通过后合并

### 响应时间

- 初始审查：通常在 3 个工作日内
- 后续审查：通常在 2 个工作日内

## 文档贡献

### 更新文档

文档位于 `documentation/zh-hans/` 目录：

- `development/` - 开发者文档
- `maintenance/` - 维护者文档

### 文档风格

- 使用中文编写
- 技术术语使用反引号标注
- 中英文之间有空格
- 代码示例格式正确

## 社区

### 讨论渠道

- GitHub Discussions - 一般讨论
- GitHub Issues - Bug 报告和功能请求

### 获取帮助

如果你在贡献过程中遇到问题：

1. 查阅现有文档
2. 搜索现有 Issues
3. 在 Discussions 中提问

## 许可证

通过贡献代码，你同意你的贡献将根据项目的 MIT 许可证授权。

## 致谢

感谢所有贡献者的付出！
