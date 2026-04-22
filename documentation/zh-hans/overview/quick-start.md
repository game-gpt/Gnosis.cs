# 快速开始

## 环境要求

### 必需组件

| 组件 | 版本要求 | 说明 |
|------|----------|------|
| .NET SDK | 8.0+ | C# 元语言层编译器与工具链 |
| Git | 最新版 | 版本控制 |

### 推荐工具

| 工具 | 用途 |
|------|------|
| Visual Studio / Rider | C# 开发与调试 |
| VS Code | gg 语言编辑 |
| RenderDoc | 渲染调试 |

---

## 获取源码

```bash
git clone https://github.com/your-org/Gnosis.cs.git
cd Gnosis.cs
```

---

## 项目结构概览

```
Gnosis.cs/
├── projects/
│   └── Gnosis/                # Gnosis 元引擎（25 个包的源码）
│       ├── Compiler/          # gg 编译器 (C#)
│       ├── Interpreter/       # 虚拟机与字节码 (C#)
│       ├── ECS/               # ECS 运行时 (C#)
│       ├── Rendering/         # 渲染系统 (C#)
│       ├── Network/           # 网络系统 (C#)
│       ├── Database/          # 嵌入式 NoSQL (C#)
│       ├── Assets/            # 资产管线 (C#)
│       ├── AI/                # AI 系统 (C#)
│       ├── Animation/         # 动画系统 (C#)
│       ├── Audio/             # 音频系统 (C#)
│       ├── Input/             # 输入系统 (C#)
│       ├── Physics/           # 物理系统 (C#)
│       ├── Editor/            # 编辑器 (C#)
│       ├── Infrastructure/    # 基础设施 (C#)
│       └── Core/              # 核心类型 (C#)
├── documentation/             # 文档
│   └── zh-hans/
└── Gnosis.sln                 # .NET 解决方案
```

> 完整的 25 包结构说明请参阅 [架构详解](../maintenance/architecture.md)。

---

## 构建项目

```bash
# 还原依赖
dotnet restore

# Debug 模式
dotnet build

# Release 模式
dotnet build -c Release
```

---

## 第一个 gg 项目

### 创建游戏配置

在项目根目录创建 `game.toml`：

```toml
[game]
name = "my-first-game"
version = "0.1.0"
author = "Your Name"

[engine]
target_arch = "WASM"

[assets]
source = "assets/"
output = "cooked/"
```

### 编写 gg 代码

创建 `scripts/main.scirpt`：

```tsx
component Position {
    x: f32;
    y: f32;
}

component Velocity {
    vx: f32;
    vy: f32;
}

system MoveSystem {
    query = Query.all(Position, Velocity);

    on_update(delta: f32) {
        <% foreach (var (pos, vel) in query) { %>
            pos.x += vel.vx * delta;
            pos.y += vel.vy * delta;
        <% } %>
    }
}

micro main() {
    game.start();
}
```

### 编译与运行

```bash
# 编译 gg 源码
dotnet run --project projects/Gnosis -- compile scripts/ --arch WASM

# 运行游戏
dotnet run --project projects/Gnosis -- run
```

---

## 核心概念

### gg 语言基础

gg 语言专为游戏逻辑设计，语法融合了 TypeScript 的简洁性与 C 的性能语义。

#### 组件 (Component)

组件是纯数据结构，不包含逻辑：

```tsx
component Health {
    current: f32;
    max: f32;
}

component Player {
    name: string;
    level: i32;
}
```

#### 系统 (System)

系统包含游戏逻辑，通过查询访问组件：

```tsx
system HealthSystem {
    query = Query.all(Health);

    on_update(delta: f32) {
        <% foreach (var health in query) { %>
            if (health.current <= 0) {
                # 处理死亡逻辑
            }
        <% } %>
    }
}
```

#### 元编程块

`<% %>` 块在编译时执行，生成特化代码：

```tsx
# 编译时循环展开
<% foreach (var entity in query) { %>
    # 这里的代码会为每个匹配的实体生成
<% } %>

# 编译时条件
<% if (MACRO.DEBUG) { %>
    debug_log("Debug mode enabled");
<% } %>
```

### Widget 系统

编辑器 UI 组件称为 **Widget**，与 ECS 的 **Component** 明确区分：

```tsx
widget Inspector {
    property selected_entity: Entity;

    render() {
        <panel title="Inspector">
            <% foreach (var comp in get_components_of(selected_entity)) { %>
                <property_field 
                    name="<%= comp.name %>"
                    value="<%= comp.value %>"
                />
            <% } %>
        </panel>
    }
}
```

---

## 多阶段构建流程

> 各阶段的详细说明与构建命令请参阅 [开发入门 - 多阶段构建流程](../development/getting-started.md#多阶段构建流程)。

---

## 插件开发

### 创建插件

```tsx
plugin WeChatChannel {
    requires_arch = ["WASM"];
    provides_macros = ["WECHAT", "WECHAT_SHARE"];
    provides_capabilities = ["WeChatLogin", "WeChatShare"];

    micro login(): Promise<UserInfo> {
        return new Promise((resolve, reject) => {
            wx_login({
                success: (res) => resolve({ code: res.code })
            });
        });
    }

    micro share(title: string, image_url: string) {
        wx_share({ title: title, image_url: image_url });
    }
}
```

### 使用插件

```tsx
<% if (MACRO.WECHAT) { %>
    import WeChatChannel;
    
    micro on_share_click() {
        WeChatChannel.share("My Game", "share_image.png");
    }
<% } %>
```

---

## 热更新

### 开发期热重载

文件变更时自动重新编译并加载：

```bash
# 启动热重载服务
dotnet run --project projects/Gnosis -- watch
```

### 发布后热更新

游戏启动时检查更新并下载：

```tsx
micro main() {
    var manifest = http.get("https://cdn.example.com/latest.json");
    
    foreach (var diff in manifest.diffs) {
        var bytes = http.download(diff.url);
        vm.load_module(diff.name, bytes);
    }
    
    game.start();
}
```

---

## 下一步

1. 阅读 [项目介绍](./introduction.md) 了解核心设计理念
2. 阅读 [设计哲学](./design-philosophy.md) 深入理解多阶段编程
3. 查看 [开发指南](../development/getting-started.md) 了解更多开发细节
4. 阅读 [架构详解](../maintenance/architecture.md) 了解 25 包结构

---

## 常见问题

### Q: gg 引擎支持哪些目标平台？

支持 Web (WASM)、Windows (x86_64)、macOS (ARM64)、iOS、Android 及主流主机平台。

### Q: 热更新是否符合 iOS App Store 政策？

是的。虚拟机是 AOT 编译的本地代码，下载的 `.code` 文件是数据，由解释器读取，完全符合商店政策。

### Q: 如何选择帧同步与状态同步？

- **帧同步**：适用于确定性逻辑、低带宽需求（如 RTS、格斗游戏）
- **状态同步**：适用于需要服务器权威校验的场景（如 RPG、FPS）

引擎支持在同一游戏中无缝切换两种模式。

### Q: Gnosis 和游戏引擎是什么关系？

Gnosis 是元引擎（Layer 1），提供 25 个 C# 包。游戏引擎（Layer 2）是基于 Gnosis 构建的 C# 应用程序。游戏内容（Layer 3）由 gg 语言编写。详见 [三层蛋糕模型](../maintenance/architecture.md#一三层蛋糕模型必须牢记)。
