# 环境搭建与项目构建

本文档介绍如何搭建 Gnosis 引擎（gg 引擎）的开发环境，以及如何构建和运行项目。

## 技术栈概览

gg 引擎采用**多阶段编程（MSP）**范式，技术栈分为三层：

| 层次 | 技术栈 | 运行时形态 | 职责 |
| :--- | :--- | :--- | :--- |
| **C# 元语言层** | C# | 构建时工具链 | 编译器、虚拟机生成器、资产管线 |
| **gg 字节码** | gg 语言 | 字节码模块 (.ggc) | 游戏逻辑、编辑器、热更新内容 |
| **C AOT 内核** | C（由 C# 生成） | 本地二进制 | 虚拟机解释器、ECS 运行时、渲染绑定 |

## 环境要求

### 必需软件

| 软件 | 版本要求 | 用途 |
| :--- | :--- | :--- |
| .NET SDK | 8.0+ | C# 元语言层编译器与工具链 |
| C 编译器 | GCC / Clang / MSVC | AOT 编译虚拟机内核 |
| Git | 任意版本 | 版本控制 |

### 推荐软件

| 软件 | 用途 |
| :--- | :--- |
| Visual Studio / Rider | C# 开发与调试 |
| VS Code | gg 语言编辑 |
| RenderDoc | 渲染调试 |

## 安装 .NET SDK

### Windows

1. 下载 [.NET SDK](https://dotnet.microsoft.com/download)
2. 运行安装程序
3. 重启终端使环境变量生效

### 验证安装

```bash
dotnet --version
```

## 克隆项目

```bash
git clone https://github.com/your-org/Gnosis.cs.git
cd Gnosis.cs
```

## 项目结构

```
Gnosis.cs/
├── src/
│   ├── Gnosis.Compiler/          # gg 编译器 (C#)
│   ├── Gnosis.VMGenerator/       # 虚拟机源码生成器 (C#)
│   ├── Gnosis.AssetPipeline/     # 资产预处理管线 (C#)
│   ├── Gnosis.BuildOrchestrator/ # 构建编排器 (C#)
│   └── Gnosis.EditorAPI/         # 编辑器 API 绑定 (C#)
├── runtime/
│   ├── gg_vm/                    # 虚拟机内核 (C, 由生成器输出)
│   ├── ecs_runtime/              # ECS 运行时 (C)
│   └── rhi/                      # 渲染硬件接口 (C)
├── editor/
│   ├── editor_main.gg            # 编辑器主场景
│   └── widgets/                  # Widget 组件 (gg)
├── examples/
│   └── my-first-game/            # 示例游戏项目
├── documentation/
│   └── zh-hans/                  # 中文文档
├── Gnosis.sln                    # .NET 解决方案
└── Readme.md
```

## 多阶段构建流程

gg 引擎将构建过程划分为六个阶段：

```
负二阶段 → 负一阶段 → 阶段〇 → 阶段一 → 阶段二 → 阶段三
```

### 阶段说明

| 阶段 | 名称 | 输入 | 输出 | 职责 |
| :--- | :--- | :--- | :--- | :--- |
| 负二阶段 | 插件配置 | gg 插件 | 宏表、能力注册表 | 加载插件，注入宏定义 |
| 负一阶段 | 资产预处理 | 原始资产 | 平台特化资产 | 格式转换、压缩优化 |
| 阶段〇 | 开发编辑 | gg 源码 | 编辑状态 | 编辑器交互、实时预览 |
| 阶段一 | 元语言执行 | gg 源码 + 宏 | 字节码 + VM 源码 | C# 编译器编译生成 |
| 阶段二 | AOT 编译 | VM 源码 (C) | 本地二进制 | C 编译器编译为可执行文件 |
| 阶段三 | 打包发布 | 字节码 + 资产 + 二进制 | 最终游戏包 | 打包、签名、分发 |

## 构建项目

### 构建 C# 元语言层

```bash
# 还原依赖
dotnet restore

# Debug 模式
dotnet build

# Release 模式
dotnet build -c Release
```

### 编译 gg 游戏项目

```bash
# 使用 gg 编译器编译游戏源码
dotnet run --project src/Gnosis.Compiler -- compile ./my-game/src --arch x64

# 输出：
# - my-game.ggc (字节码)
# - vm_generated/ (虚拟机 C 源码)
```

### AOT 编译虚拟机内核

```bash
# 使用 C 编译器编译生成的虚拟机源码
# Windows (MSVC)
cl /O2 /Fe:game.exe vm_generated/*.c runtime/*.c

# Linux/macOS (GCC/Clang)
gcc -O2 -o game vm_generated/*.c runtime/*.c
```

### 一键构建

```bash
# 使用构建编排器完成完整构建流程
dotnet run --project src/Gnosis.BuildOrchestrator -- publish \
    --source ./my-game/src \
    --arch x64 \
    --platform windows \
    --output ./dist
```

## 运行测试

```bash
# 运行 C# 单元测试
dotnet test

# 运行特定项目的测试
dotnet test src/Gnosis.Compiler.Tests
```

## 运行示例

### 启动编辑器

```bash
# 启动编辑器（编辑器本身由 gg 编写，运行于虚拟机之上）
dotnet run --project src/Gnosis.Editor -- ./examples/my-first-game
```

### 运行游戏

```bash
# 直接运行编译后的游戏
./dist/game.exe
```

## 开发工作流

### 热重载（开发期）

编辑器支持 gg 源码的热重载：

1. 修改 `.gg` 源文件
2. C# 元语言服务自动增量编译
3. 虚拟机替换内存中的模块定义
4. ECS 世界自动使用新定义

### 热更新（发布后）

游戏启动时检查更新：

```tsx
export function main() {
    var manifest = http.get("https://cdn.example.com/latest.json");
    foreach (var diff in manifest.diffs) {
        var bytes = http.download(diff.url);
        vm.load_module(diff.name, bytes);
    }
    game.start();
}
```

## IDE 配置

### Visual Studio / Rider

推荐配置：

- 启用 C# 代码分析
- 配置 `.editorconfig` 格式化规则
- 安装 gg 语言插件（可选）

### VS Code

推荐安装以下扩展：

- C# Dev Kit
- gg Language Support（自定义插件）
- Better TOML

### 配置文件

`.vscode/settings.json`:

```json
{
    "dotnet.defaultSolution": "Gnosis.sln",
    "editor.formatOnSave": true,
    "files.associations": {
        "*.gg": "gg",
        "*.ggs": "gg-shader"
    }
}
```

## 常见问题

### 编译错误：找不到 .NET SDK

确保已安装 .NET SDK 8.0 或更高版本，并重启终端。

### AOT 编译错误：找不到 C 编译器

- **Windows**: 安装 Visual Studio Build Tools 或 MSVC
- **Linux**: 安装 `gcc` 或 `clang`
- **macOS**: 安装 Xcode Command Line Tools

### 运行时错误：图形设备不支持

确保系统支持 Vulkan、Metal 或 DirectX，并安装了最新的显卡驱动。

### 热更新失败

检查网络连接和 CDN 配置，确保 `.ggc` 字节码文件格式正确。

## 下一步

- 阅读 [架构设计](architecture.md) 了解多阶段编程模型
- 阅读 [gg 语言指南](gg-language.md) 学习 ECS 编程
- 阅读 [渲染系统](rendering.md) 了解 RHI 抽象层
- 查看 [示例项目](../../examples/) 了解实际用法
