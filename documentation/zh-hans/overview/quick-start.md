# 快速开始

## 环境要求

### 必需组件

| 组件 | 版本要求 | 说明 |
|------|----------|------|
| Rust | 1.70+ | 核心运行时环境 |
| .NET SDK | 8.0+ | 元语言编译器依赖 |
| Git | 最新版 | 版本控制 |

### 推荐工具

| 工具 | 用途 |
|------|------|
| VS Code / Rider | IDE 开发环境 |
| Docker | 容器化部署 |

---

## 获取源码

```bash
# 克隆仓库
git clone https://github.com/your-org/gnosis-engine.git

# 进入项目目录
cd gnosis-engine
```

---

## 项目结构概览

```
gnosis-engine/
├── projects/
│   ├── gg-engine-galgame/      # Galgame 游戏模板
│   ├── gg-engine-platformer/   # 平台跳跃游戏模板
│   ├── gg-engine-stg/          # STG 弹幕游戏模板
│   ├── my-first-galgame/       # 示例项目
│   └── wion/                   # 数据序列化库
├── documentation/              # 文档
│   └── zh-hans/
│       ├── overview/           # 概览
│       ├── development/        # 开发指南
│       └── maintenance/        # 维护指南
└── Cargo.toml                  # Rust 工作空间配置
```

---

## 构建项目

```bash
# 还原依赖
cargo build

# 运行测试
cargo test

# 构建发布版本
cargo build --release
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
target_arch = "WASM"  # 支持: WASM, x86_64, ARM64

[assets]
source = "assets/"
output = "cooked/"
```

### 编写 gg 代码

创建 `scripts/main.scirpt`：

```tsx
// 组件定义
export component Position {
    x: float;
    y: float;
}

export component Velocity {
    vx: float;
    vy: float;
}

// 系统定义
export system MoveSystem {
    query = Query.all(Position, Velocity);

    on_update(delta: float) {
        <% foreach (var (pos, vel) in query) { %>
            pos.x += vel.vx * delta;
            pos.y += vel.vy * delta;
        <% } %>
    }
}

// 游戏入口
export function main() {
    game.start();
}
```

### 编译与运行

```bash
# 编译 gg 源码
ggc compile scripts/ --arch WASM

# 运行游戏
ggc run
```

---

## 核心概念

### gg 语言基础

gg 语言专为游戏逻辑设计，语法融合了 TypeScript 的简洁性与 C 的性能语义。

#### 组件 (Component)

组件是纯数据结构，不包含逻辑：

```tsx
export component Health {
    current: float;
    max: float;
}

export component Player {
    name: string;
    level: int;
}
```

#### 系统 (System)

系统包含游戏逻辑，通过查询访问组件：

```tsx
export system HealthSystem {
    query = Query.all(Health);

    on_update(delta: float) {
        <% foreach (var health in query) { %>
            if (health.current <= 0) {
                // 处理死亡逻辑
            }
        <% } %>
    }
}
```

#### 元编程块

`<% %>` 块在编译时执行，生成特化代码：

```tsx
// 编译时循环展开
<% foreach (var entity in query) { %>
    // 这里的代码会为每个匹配的实体生成
<% } %>

// 编译时条件
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

    export function login(): Promise<UserInfo> {
        return new Promise((resolve, reject) => {
            wx_login({
                success: (res) => resolve({ code: res.code })
            });
        });
    }

    export function share(title: string, image_url: string) {
        wx_share({ title: title, image_url: image_url });
    }
}
```

### 使用插件

```tsx
<% if (MACRO.WECHAT) { %>
    import WeChatChannel;
    
    function on_share_click() {
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
ggc watch
```

### 发布后热更新

游戏启动时检查更新并下载：

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

---

## 下一步

1. 阅读 [项目介绍](./introduction.md) 了解核心设计理念
2. 阅读 [设计哲学](./design-philosophy.md) 深入理解多阶段编程
3. 查看 [开发指南](../development/getting-started.md) 了解更多开发细节
4. 探索 `projects/` 目录下的示例项目

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
