# gg 语言语法与 ECS 编程指南

本文档介绍 gg 语言的语法特性，以及如何使用 gg 语言进行 ECS 编程。

## 语言概述

gg 语言专为游戏逻辑设计，具有以下特性：

| 特性 | 描述 |
| :--- | :--- |
| TypeScript 风格语法 | 简洁易学，类型安全 |
| 原生 ECS 支持 | 组件、系统、查询一等公民 |
| 编译时元编程 | `<% %>` 块在编译时执行 |
| 零运行时反射 | 所有类型信息在编译时解析 |

## 基础语法

### 变量声明

```tsx
var x: int = 42;
var y: float = 3.14;
var name: string = "hello";
var flag: bool = true;
```

### 函数定义

```tsx
function add(a: int, b: int): int {
    return a + b;
}

export function main() {
    var result = add(1, 2);
}
```

### 控制流

```tsx
if (x > 0) {
    // ...
} else {
    // ...
}

for (var i = 0; i < 10; i++) {
    // ...
}

while (condition) {
    // ...
}
```

## 组件定义

组件是纯数据容器，不包含任何逻辑：

```tsx
export component Position {
    x: float;
    y: float;
}

export component Velocity {
    vx: float;
    vy: float;
}

export component Health {
    current: int = 100;
    max: int = 100;
}

export component PlayerTag {
    player_id: int;
}
```

### 组件属性

```tsx
@encrypted
export component PlayerData {
    gold: int;
    level: int;
    
    @honeypot(trigger = "on_cheat_suspected")
    _gold_fake: int;
}
```

| 属性 | 描述 |
| :--- | :--- |
| `@encrypted` | 内存加密保护 |
| `@honeypot` | 蜜罐陷阱 |
| `@replicated` | 网络复制 |

## 系统定义

系统包含游戏逻辑，通过查询访问组件：

```tsx
export system MoveSystem {
    query = Query.all(Position, Velocity);

    on_update(delta: float) {
        <% foreach (var (pos, vel) in query) { %>
            pos.x += vel.vx * delta;
            pos.y += vel.vy * delta;
        <% } %>
    }
}
```

### 系统生命周期

```tsx
export system MySystem {
    on_load() {
        // 系统加载时调用
    }
    
    on_update(delta: float) {
        // 每帧调用
    }
    
    on_unload() {
        // 系统卸载时调用
    }
}
```

### 网络系统

```tsx
@server_only
export function handle_attack(player: Entity, target: Entity) {
    // 仅在服务器执行
}

system ServerMovement {
    on_server_update(delta: float) {
        // 服务器权威逻辑
    }
}

system ClientPredictionMovement {
    on_client_update(delta: float) {
        // 客户端预测逻辑
    }
    
    on_receive_server_state(msg: ServerStateMessage) {
        // 和解逻辑
    }
}
```

## 查询机制

### 查询类型

| 查询 | 描述 |
| :--- | :--- |
| `Query.all(A, B)` | 包含所有指定组件 |
| `Query.any(A, B)` | 包含任意指定组件 |
| `Query.none(A)` | 不包含指定组件 |

### 查询示例

```tsx
system DamageSystem {
    query_all = Query.all(Health, Damage);
    query_players = Query.all(PlayerTag, Health);
    query_enemies = Query.all(EnemyTag).none(PlayerTag);

    on_update(delta: float) {
        <% foreach (var (health, damage) in query_all) { %>
            health.current -= damage.value;
        <% } %>
    }
}
```

## 编译时元编程

`<% %>` 块在编译时执行，用于代码生成：

### 静态循环展开

```tsx
on_update(delta: float) {
    <% foreach (var (pos, vel) in query) { %>
        pos.x += vel.vx * delta;
        pos.y += vel.vy * delta;
    <% } %>
}
```

### 条件编译

```tsx
<% if (MACRO.NET_BACKEND == "STEAM") { %>
    import SteamMock;
    type NetBackend = SteamBackend;
<% } else if (MACRO.NET_BACKEND == "WEBSOCKET") { %>
    import WebSocketMock;
    type NetBackend = WebSocketBackend;
<% } %>
```

### 宏展开

```tsx
<% foreach (var i in range(0, 10)) { %>
    var value_<%= i %> = <%= i * 2 %>;
<% } %>
```

### 编译时 ECS 特化

`<% foreach %>` 并非运行时循环，而是**编译时宏展开**。gg 编译器分析 ECS 世界的 Archetype 结构，生成针对特定组件组合的线性遍历代码，避免虚调用与缓存未命中。

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

编辑器本身完全由 gg 语言编写，运行于 gg 虚拟机之上。UI 组件称为 **Widget**，与 ECS 的 **Component** 明确区分。

### Widget 与 Component 区别

| 概念 | 定义位置 | 用途 | 运行时表现 |
| :--- | :--- | :--- | :--- |
| **Widget** | 编辑器 gg 模块 | 绘制编辑器用户界面 | 由 UI 渲染系统绘制，不进入游戏世界 |
| **Component** | 游戏 gg 模块 | 存储游戏实体数据 | 存储于 ECS 世界，由系统查询并处理 |

### Inspector Widget 示例

```tsx
widget Inspector {
    property selected_entity: Entity;

    render() {
        <% foreach (var comp in get_components_of(selected_entity)) { %>
            <collapsible_section title="<%= comp.name %>">
                <% foreach (var prop in comp.properties) { %>
                    <% if (prop.type == "float") { %>
                        <float_field 
                            label="<%= prop.name %>"
                            value="<%= selected_entity.get_float(comp.name, prop.name) %>"
                            on_change="(v) => selected_entity.set_float(comp.name, prop.name, v)"
                        />
                    <% } else if (prop.type == "int") { %>
                        <int_field ... />
                    <% } %>
                <% } %>
            </collapsible_section>
        <% } %>
    }
}
```

### 编辑器主场景示例

```tsx
export scene EditorMain {
    on_load() {
        editor_core.init();
        this.ui_root = widget_dock_layout.create({
            children: [
                widget_main_menu.create(),
                widget_toolbar.create(),
                widget_content_browser.create(),
                widget_viewport_3d.create(),
                widget_inspector.create()
            ]
        });
    }

    on_update(delta: float) {
        editor_input.process_shortcuts();
    }
}
```

## 实体操作

### 创建实体

```tsx
var entity = create_entity();
entity.add(Position { x: 100.0, y: 200.0 });
entity.add(Velocity { vx: 0.0, vy: 0.0 });
```

### 销毁实体

```tsx
destroy_entity(entity);
```

### 组件访问

```tsx
var pos = entity.get<Position>();
pos.x = 150.0;

if (entity.has<Health>()) {
    var health = entity.get<Health>();
    health.current -= 10;
}
```

## 场景定义

```tsx
export scene GameMain {
    var player: Entity;

    on_load() {
        player = create_entity();
        player.add(PlayerTag { player_id: 0 });
        player.add(Position { x: 100.0, y: 100.0 });
    }

    on_update(delta: float) {
        // 游戏逻辑
    }
}
```

## 插件定义

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

- 阅读 [网络架构](network.md) 了解帧同步与状态同步
- 阅读 [反作弊体系](anti-cheat.md) 了解安全防护
- 查看 [示例项目](../../examples/) 了解实际用法
