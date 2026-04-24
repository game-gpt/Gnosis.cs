# Valkyrie 语言语法与 ECS 编程指南

本文档介绍 Valkyrie 语言的语法特性，以及如何使用 Valkyrie 语言进行 ECS 编程。

## ⚠️ 语言归属声明

**Valkyrie 是游戏对象语言，不是 C#。** 以下概念在 Valkyrie 语言中与 C# 完全不同，切勿混淆：

| Valkyrie 语言概念 | Valkyrie 语法示例 | 易混淆的 C# 概念 | 区别 |
|---|---|---|---|
| Valkyrie 特性标注 | `[Encrypted]`、`[Replicated]` | C# `System.Attribute` | Valkyrie 特性标注由编译器在编译时处理，不依赖 C# 反射 |
| ValkyrieShader 特性标注 | `[Vertex]`、`[Compute]`、`[WorkgroupSize]` | C# `System.Attribute` | ValkyrieShader 特性标注定义着色器入口和管线语义 |
| 成员访问 | `person.name` | C# 属性 (Property) | Valkyrie 语言只有字段 (Field)，没有 C# 意义上的属性 |
| 元编程块 | `<% %>` | C# 源生成器 | `<% %>` 内部是 C# 语法，由 C# 元语言层在编译时执行 |

**核心原则**：无论是游戏本体、插件 (Plugin)、Mod、DLC，还是编辑器 Widget，都使用 Valkyrie、ValkyrieShader、AWSL 编写，**而非 C#**。C# 仅用于 Layer 1（元引擎）和 Layer 2（游戏引擎）。详见 [项目介绍 - 三层蛋糕模型](../overview/introduction.md#⚠️-关键概念三层蛋糕模型)。

## 语言概述

Valkyrie 专为游戏逻辑设计，具有以下特性：

| 特性              | 描述              |
| :-------------- | :-------------- |
| 原生 ECS 支持       | 组件、系统、查询一等公民    |
| 编译时元编程          | `<% %>` 块在编译时执行 |
| 零运行时反射          | 所有类型信息在编译时解析    |

## 基础语法

### 注释

Valkyrie 支持两种注释语法：

| 语法 | 描述 |
| :--- | :--- |
| `#` | 行注释，从 `#` 到行末的内容被忽略 |
| `<# #>` | 块注释，支持嵌套 |

```valkyrie
# 这是行注释
let x = 1; # 行末注释

<# 这是块注释 #>
let y = 2;

<# 嵌套
   <# 内层注释 #>
#>
let z = 3;
```

### 变量声明

```valkyrie
let x: i32 = 42;
let mut y: f32 = 3.14;
let name: string = "hello";
let flag: bool = true;
```

### 函数定义

```valkyrie
micro add(a: i32, b: i32): i32 {
    return a + b;
}

micro main() {
    let result = add(1, 2);
}
```

### 控制流

```valkyrie
if x > 0 {
    # ...
} else {
    # ...
}

loop {
    # ...
}

loop item in collection {
    # ...
}

while condition {
    # ...
}
```

## 组件定义

组件是纯数据容器，不包含任何逻辑：

```valkyrie
component Position {
    x: f32;
    y: f32;
}

component Velocity {
    vx: f32;
    vy: f32;
}

component Health {
    current: i32 = 100;
    max: i32 = 100;
}

component PlayerTag {
    player_id: i32;
}
```

### 组件属性

```valkyrie
[Encrypted]
component PlayerData {
    gold: int;
    level: int;
    
    [Honeypot(trigger = "on_cheat_suspected")]
    _gold_fake: int;
}
```

| 属性            | 描述     |
| :------------ | :----- |
| `[Encrypted]`  | 内存加密保护 |
| `[Honeypot]`   | 蜜罐陷阱   |
| `[Replicated]` | 网络复制   |

## 系统定义

系统包含游戏逻辑，通过查询访问组件：

```valkyrie
system MoveSystem {
    query = Query.all(Position, Velocity);

    on_update(delta: f32) {
        <% loop (pos, vel) in query %>
            pos.x += vel.vx * delta;
            pos.y += vel.vy * delta;
        <% end loop %>
    }
}
```

### 系统生命周期

```valkyrie
system MySystem {
    on_load() {
        # 系统加载时调用
    }
    
    on_update(delta: f32) {
        # 每帧调用
    }
    
    on_unload() {
        # 系统卸载时调用
    }
}
```

### 网络系统

```valkyrie
[ServerOnly]
micro handle_attack(player: Entity, target: Entity) {
    # 仅在服务器执行
}

system ServerMovement {
    on_server_update(delta: f32) {
        # 服务器权威逻辑
    }
}

system ClientPredictionMovement {
    on_client_update(delta: f32) {
        # 客户端预测逻辑
    }
    
    on_receive_server_state(msg: ServerStateMessage) {
        # 和解逻辑
    }
}
```

## 查询机制

### 查询类型

| 查询                | 描述       |
| :---------------- | :------- |
| `Query.all(A, B)` | 包含所有指定组件 |
| `Query.any(A, B)` | 包含任意指定组件 |
| `Query.none(A)`   | 不包含指定组件  |

### 查询示例

```valkyrie
system DamageSystem {
    query_all = Query.all(Health, Damage);
    query_players = Query.all(PlayerTag, Health);
    query_enemies = Query.all(EnemyTag).none(PlayerTag);

    on_update(delta: f32) {
        <% loop (health, damage) in query_all %>
            health.current -= damage.value;
        <% end loop %>
    }
}
```

## 编译时元编程

`<% %>` 块在编译时执行，用于代码生成：

### 静态循环展开

```valkyrie
on_update(delta: float) {
    <% loop (pos, vel) in query %>
        pos.x += vel.vx * delta;
        pos.y += vel.vy * delta;
    <% end loop %>
}
```

### 条件编译

```valkyrie
<% if (MACRO.NET_BACKEND == "STEAM") { %>
    import SteamMock;
    type NetBackend = SteamBackend;
<% } else if (MACRO.NET_BACKEND == "WEBSOCKET") { %>
    import WebSocketMock;
    type NetBackend = WebSocketBackend;
<% } %>
```

### 循环语法

```valkyrie
<% loop i in range(0, 10) %>
    let value_<%= i %> = <%= i * 2 %>;
<% end loop %>
```

### 模式匹配

```valkyrie
<% match value %>
    <% case 0 %>
        # 处理 0 的情况
    <% case 1 %>
        # 处理 1 的情况
    <% case _ %>
        # 处理其他情况
<% end match %>
```

### 编译时 ECS 特化

`<% loop %>` 并非运行时循环，而是**编译时宏展开**。Valkyrie 编译器分析 ECS 世界的 Archetype 结构，生成针对特定组件组合的线性遍历代码，避免虚调用与缓存未命中。

**生成的字节码 (伪汇编)**：

```
GET_ARCHETYPE R0, Archetype_PosVel
LOOP_START:
LD_FIELD R1, R0, offsetof(Position.x)
LD_FIELD R2, R0, offsetof(Velocity.vx)
MUL_F R3, R2, delta
ADD_F R1, R1, R3
ST_FIELD R0, offsetof(Position.x), R1
...
```

## Widget 系统

编辑器本身完全由 Valkyrie 语言编写，运行于 Valkyrie 虚拟机之上。UI 组件称为 **Widget**，与 ECS 的 **Component** 明确区分。

> **注意**：Widget 仅用于编辑器 UI。游戏运行时 UI（HUD、血条、技能轮盘等）使用独立的 **Game UI** 系统，基于 ECS 组件和系统构建，与 Widget 是两套完全不同的体系。详见 [AWSL 语言指南 - Game UI 系统](awsl.md#game-ui-系统)。

### Widget 与 Component 区别

| 概念            | 定义位置      | 用途        | 运行时表现               |
| :------------ | :-------- | :-------- | :------------------ |
| **Widget**    | 编辑器 Valkyrie 模块 | 绘制编辑器用户界面 | 由 UI 渲染系统绘制，不进入游戏世界 |
| **Component** | 游戏 Valkyrie 模块  | 存储游戏实体数据  | 存储于 ECS 世界，由系统查询并处理 |

### Inspector Widget 示例

> 完整的 Inspector Widget 示例请参阅 [编辑器架构 - Inspector Widget](../development/editor.md#inspector-widget-完整示例)。

### 编辑器主场景示例

> 编辑器主场景的完整实现请参阅 [编辑器架构 - 编辑器主场景](../development/editor.md#编辑器主场景)。

## 实体操作

### 创建实体

```valkyrie
let entity = create_entity();
entity.add(Position { x: 100.0, y: 200.0 });
entity.add(Velocity { vx: 0.0, vy: 0.0 });
```

### 销毁实体

```valkyrie
destroy_entity(entity);
```

### 组件访问

```valkyrie
let pos = entity.get<Position>();
pos.x = 150.0;

if entity.has<Health>() {
    let health = entity.get<Health>();
    health.current -= 10;
}
```

## 场景定义

场景（`.scene`）是 GON 格式的序列化文件，不属于脚本语言的声明。场景数据通过 `asset.load<SceneData>()` 加载，由引擎的场景管理器负责实例化和生命周期调度。

场景文件示例（`assets/scenes/game_main.scene`）：

```gon
Scene {
    name: "GameMain",
    entities: [
        Entity {
            name: "Player",
            components: [
                PlayerTag { player_id: 0 },
                Position { x: 100.0, y: 100.0 }
            ]
        }
    ],
    systems: [
        SceneSystem { type_name: "MoveSystem", is_enabled: true }
    ],
    environment: {
        background_color: "#1a1a2e",
        gravity: 9.8
    }
}
```

场景的加载与切换：

```valkyrie
micro on_load() {
    let scene = asset.load<SceneData>("scenes/game_main.scene");
    scene_manager.load(scene);
}

micro on_update(delta: f32) {
    # 游戏逻辑
}
```

> 有关 Gon 格式的详细语法，请参阅 [Gon 语言语法指南](gon.md)。

## 插件定义

```valkyrie
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
}
```

## 最佳实践

### 组件设计

- 组件应该是纯数据，不包含逻辑
- 组件应该小而专注
- 避免组件之间的依赖

### 系统设计

- 系统应该是纯逻辑，不存储状态
- 系统之间应该通过组件通信
- 避免系统之间的直接依赖

### 性能优化

- 使用 Archetype 友好的组件组合
- 避免频繁的组件添加/删除
- 使用查询缓存

## 下一步

- 阅读 [网络架构](../development/network.md) 了解帧同步与状态同步
- 阅读 [反作弊体系](../development/anti-cheat.md) 了解安全防护
- 查看 [示例项目](../../examples/) 了解实际用法
