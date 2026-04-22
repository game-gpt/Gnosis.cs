# 开发入门

## 环境配置

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
| RenderDoc | 渲染调试 |
| Wireshark | 网络调试 |

---

## 项目结构

```
Gnosis.cs/
├── projects/
│   └── Gnosis/                    # Gnosis 元引擎源码
│       ├── Compiler/              # Gnosis.IR + Gnosis.Toolchain
│       │   ├── AST/               # 抽象语法树
│       │   ├── Backend/           # 字节码/SPIR-V 发射器
│       │   ├── Cache/             # 编译缓存
│       │   ├── Diagnostics/       # 诊断信息
│       │   ├── Lexer/             # 词法分析
│       │   ├── Meta/              # 元编程
│       │   ├── Parser/            # 语法分析
│       │   ├── ScriptFrontend/    # gg-script 前端
│       │   └── ShaderFrontend/    # gg-shader 前端
│       ├── Interpreter/           # Gnosis.Runtime + Gnosis.IR
│       │   ├── IR/                # 中间表示
│       │   └── VM/                # 虚拟机
│       ├── ECS/                   # Gnosis.ECS
│       │   ├── Core/              # 实体与组件核心
│       │   ├── Implementation/    # 系统与查询实现
│       │   └── Interface/         # 公共接口
│       ├── Rendering/             # Gnosis.Graphic
│       │   ├── Backends/          # Vulkan/Metal/D3D12/Software
│       │   ├── GameUI/            # Gnosis.GameUI 子系统
│       │   ├── Lighting/          # 光照系统
│       │   ├── Particles/         # 粒子系统
│       │   ├── Pipeline/          # 渲染管线
│       │   ├── RHI/               # 硬件抽象层
│       │   ├── Shader/            # 着色器系统
│       │   └── ShaderGenerator/   # 着色器代码生成
│       ├── Network/               # Gnosis.Network
│       │   ├── Backends/          # 传输后端
│       │   ├── Core/              # 网络核心
│       │   ├── Messages/          # 消息定义
│       │   └── Sync/              # 同步系统
│       ├── Database/              # Gnosis.Database
│       │   ├── BTree/             # B+ 树存储引擎
│       │   ├── Core/              # 数据库核心
│       │   ├── PageManager/       # 页面管理
│       │   ├── SHM/               # 共享内存
│       │   ├── Storage/           # 存储层
│       │   └── WAL/               # 预写日志
│       ├── Assets/                # Gnosis.Asset
│       │   ├── Formats/           # 格式解析器
│       │   ├── Loading/           # 资产加载
│       │   └── VFS/               # 虚拟文件系统
│       ├── AI/                    # Gnosis.AI + Gnosis.Navigation
│       │   ├── BehaviorTree/      # 行为树
│       │   ├── Navigation/        # 导航系统
│       │   └── Perception/        # 感知系统
│       ├── Animation/             # Gnosis.Animation
│       ├── Audio/                 # Gnosis.Audio
│       ├── Input/                 # Gnosis.Input
│       ├── Physics/               # Gnosis.Physics
│       ├── Editor/                # Gnosis.Widget (编辑器)
│       ├── Infrastructure/        # Gnosis.Core 子模块
│       └── Core/                  # Gnosis.Core
├── documentation/                 # 文档
│   └── zh-hans/
└── Gnosis.sln                     # .NET 解决方案
```

> 上述目录映射了新架构的 25 个包。完整的包结构说明请参阅 [架构详解](../maintenance/architecture.md)。

---

## 构建流程

### 基本构建

```bash
# 还原依赖
dotnet restore

# Debug 模式
dotnet build

# Release 模式
dotnet build -c Release
```

### 运行测试

```bash
# 运行所有测试
dotnet test

# 运行特定项目测试
dotnet test projects/Gnosis.Tests
```

---

## 多阶段构建流程

### 阶段概览

| 阶段 | 名称 | 执行者 | 输入 | 输出 |
|------|------|--------|------|------|
| **负二阶段** | 插件配置 | C# 元语言 | gg 插件 | 全局宏表、能力注册表 |
| **负一阶段** | 资产预处理 | C# 资产管线 | 原始资产 | 平台特化资产 |
| **阶段〇** | 开发编辑 | 编辑器 (gg) | gg 源码、资产 | 编辑后的项目 |
| **阶段一** | 元语言执行 | C# 编译器 | gg 源码 | 字节码 |
| **阶段二** | 打包构建 | 打包器 | 字节码 + 资产 | 平台包 |
| **阶段三** | 发布部署 | 部署器 | 平台包 | 最终游戏包 |

### 构建命令

```bash
# 完整构建（从负二阶段到阶段二）
dotnet run --project projects/Gnosis -- build --target WASM

# 仅编译 gg 源码（阶段一）
dotnet run --project projects/Gnosis -- compile scripts/ --arch WASM

# 打包（阶段二）
dotnet run --project projects/Gnosis -- package --target WASM --output dist/

# 部署到设备（阶段三）
dotnet run --project projects/Gnosis -- deploy --target WASM --device local
```

---

## 开发工作流

### 引擎开发者（Layer 1/2）

引擎开发者使用 C# 编写 Gnosis 包的代码：

1. 修改 `projects/Gnosis/` 下的 C# 源码
2. 运行 `dotnet build` 确保编译通过
3. 运行 `dotnet test` 确保测试通过
4. 提交代码

### 游戏开发者（Layer 3）

游戏开发者使用 GG 语言族编写游戏内容：

1. 编写 `.script`（gg-script）、`.shader`（gg-shader）、`.widget`（gg-widget）文件
2. 通过编辑器或 CLI 编译
3. 运行与调试

---

## 调试

### C# 层调试

使用 Visual Studio / Rider 的标准调试器：

```bash
# 启动调试模式
dotnet run --project projects/Gnosis -- --debug
```

### gg 层调试

通过编辑器的调试工具：

- 断点设置
- 单步执行
- 变量监视
- ECS 组件检查器

---

## 常见问题

### Q: 为什么项目结构和新架构的 25 包不完全对应？

当前代码目录是历史演化的结果，新架构的 25 包是设计目标。代码目录会逐步重构对齐。详见 [架构详解](../maintenance/architecture.md)。

### Q: 如何添加新的 Gnosis 包？

1. 在 `projects/Gnosis/` 下创建对应的目录
2. 遵循 [架构详解](../maintenance/architecture.md) 中的命名规范
3. 确保依赖方向正确：基础层 ← 核心层 ← 子系统层

### Q: C# 和 gg-script 的边界在哪里？

C# 用于 Layer 1（元引擎）和 Layer 2（游戏引擎），gg-script 用于 Layer 3（游戏内容）。详见 [项目介绍 - 三层蛋糕模型](../overview/introduction.md#⚠️-关键概念三层蛋糕模型)。
