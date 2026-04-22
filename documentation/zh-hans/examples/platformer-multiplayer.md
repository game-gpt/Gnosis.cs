# 双人横版闯关联网示例

本文档演示如何使用 gg 语言实现一个完整的双人横版闯关联网游戏，包含帧同步（攻击判定）与状态同步（位置移动）的切换，以及通过宏控制 Steam / WebSocket 后端切换。

## 示例概述

| 特性 | 描述 |
| :--- | :--- |
| 网络后端 | 支持 Steam P2P、WebSocket、单机模式 |
| 同步模式 | 帧同步战斗 + 状态同步移动 |
| 运行切换 | 通过 UI 按钮切换同步模式 |
| 热重载 | 模式切换后立即生效 |

## 架构概览

```
┌─────────────────────────────────────────────────────────┐
│                      GameMain Scene                      │
├─────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │
│  │ PlayerTag   │  │ NetTransform│  │ CombatStats │     │
│  └─────────────┘  └─────────────┘  └─────────────┘     │
├─────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────┐   │
│  │              NetworkManager                       │   │
│  │  ┌───────────┐  ┌───────────┐  ┌───────────┐   │   │
│  │  │SteamBackend│  │WebSocket  │  │NullBackend│   │   │
│  │  └───────────┘  └───────────┘  └───────────┘   │   │
│  └─────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────┤
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │
│  │ 状态同步移动 │  │ 帧同步战斗  │  │ 输入收集    │     │
│  │ ServerMove  │  │LockstepCombat│ │InputCollect │     │
│  │ClientPredict│  │             │  │             │     │
│  └─────────────┘  └─────────────┘  └─────────────┘     │
└─────────────────────────────────────────────────────────┘
```

## 网络后端抽象与插件切换

### Steam 网络后端

```tsx
plugin SteamMock {
    requires_arch = ["WIN64"];
    provides_capabilities = ["P2P_Reliable", "P2P_Unreliable"];

    export class SteamBackend {
        var connected_peers: map<uint64, PeerState>;

        export function send_reliable(target: uint64, data: byte[]) {
            steam_sdk_send_reliable(target, data);
        }

        export function send_unreliable(target: uint64, data: byte[]) {
            steam_sdk_send_unreliable(target, data);
        }

        export function receive(): Message[] {
            return steam_sdk_poll_messages();
        }
    }
}
```

### WebSocket 网络后端

```tsx
plugin WebSocketMock {
    requires_arch = ["WASM"];
    provides_capabilities = ["ClientServer"];

    export class WebSocketBackend {
        var socket: WebSocketHandle;

        export function connect(url: string) {
            socket = websocket_connect(url);
        }

        export function send(data: byte[]) {
            websocket_send(socket, data);
        }

        export function receive(): Message[] {
            return websocket_poll(socket);
        }
    }
}
```

### 后端对比

| 后端 | 平台 | 传输模式 | 适用场景 |
| :--- | :--- | :--- | :--- |
| Steam | PC (Steam) | P2P 可靠/不可靠 | Steam 平台游戏 |
| WebSocket | Web / 专用服务器 | 客户端-服务器 | Web 游戏、专用服务器 |
| None | 全平台 | 无 | 单机模式 |

## 网络管理器

通过编译时宏选择网络后端：

```tsx
<% if (MACRO.NET_BACKEND == "STEAM") { %>
    import SteamMock;
    type NetBackend = SteamBackend;
<% } else if (MACRO.NET_BACKEND == "WEBSOCKET") { %>
    import WebSocketMock;
    type NetBackend = WebSocketBackend;
<% } else { %>
    type NetBackend = NullBackend;
<% } %>

export class NetworkManager {
    static var backend: NetBackend;
    static var local_player_id: int = -1;
    static var is_server: bool = false;

    static function init() {
        <% if (MACRO.NET_BACKEND != "NONE") { %>
            backend = new NetBackend();
        <% } %>
    }

    static function send_reliable(target: int, msg_id: int, data: byte[]) {
        var packet = pack_message(msg_id, data);
        <% if (MACRO.NET_BACKEND == "STEAM") { %>
            backend.send_reliable(target, packet);
        <% } else if (MACRO.NET_BACKEND == "WEBSOCKET") { %>
            backend.send(packet);
        <% } %>
    }

    static function send_unreliable(target: int, msg_id: int, data: byte[]) {
        var packet = pack_message(msg_id, data);
        <% if (MACRO.NET_BACKEND == "STEAM") { %>
            backend.send_unreliable(target, packet);
        <% } else { %>
            backend.send(packet);
        <% } %>
    }

    static function poll(): Message[] {
        <% if (MACRO.NET_BACKEND != "NONE") { %>
            var raw = backend.receive();
            return parse_messages(raw);
        <% } else { %>
            return [];
        <% } %>
    }
}
```

## 玩家组件与输入收集

### 玩家组件定义

```tsx
export component PlayerTag {
    player_id: int;
    local_device: int;
}

export component PlayerInput {
    move_left: bool;
    move_right: bool;
    jump: bool;
    attack: bool;
    frame: int;
}

[Replicated(Authority.Server, Interpolation.Linear)]
export component NetTransform {
    x: float;
    y: float;
    vx: float;
    vy: float;
}

export component PredictedState {
    predicted_x: float;
    predicted_y: float;
    last_server_x: float;
    last_server_y: float;
    server_frame: int;
}

export component CombatStats {
    health: int = 100;
    attack_damage: int = 20;
    attack_cooldown: float = 0.0;
}

export component AttackEvent {
    attacker_id: int;
    target_id: int;
    damage: int;
    frame: int;
}
```

### 输入收集系统

```tsx
system InputCollection {
    query = Query.all(PlayerTag);

    on_update() {
        foreach (var entity in query) {
            var tag = entity.get<PlayerTag>();
            var raw = get_raw_input(tag.local_device);
            
            var input = entity.get_or_add<PlayerInput>();
            input.move_left  = raw.key_left;
            input.move_right = raw.key_right;
            input.jump       = raw.key_space;
            input.attack     = raw.key_j;
            input.frame      = get_current_logic_frame();
            
            if (NetworkManager.is_server || !is_authoritative_mode()) {
                NetworkManager.send_unreliable(
                    NetworkManager.local_player_id, 
                    MSG_PLAYER_INPUT, 
                    serialize(input)
                );
            }
        }
    }
}
```

## 状态同步移动系统

状态同步采用服务器权威模式，客户端进行预测与和解。

### 架构图

```
┌─────────────┐         ┌─────────────┐
│   客户端 A   │         │   服务器    │
│             │         │             │
│ ┌─────────┐ │  输入   │ ┌─────────┐ │
│ │预测移动  │ │ ─────→ │ │权威移动  │ │
│ └─────────┘ │         │ └─────────┘ │
│             │         │             │
│ ┌─────────┐ │  状态   │ ┌─────────┐ │
│ │和解逻辑  │ │ ←───── │ │状态广播  │ │
│ └─────────┘ │         │ └─────────┘ │
└─────────────┘         └─────────────┘
```

### 服务器权威移动

```tsx
const MOVE_SPEED = 300.0;
const GRAVITY = 980.0;
const JUMP_FORCE = -500.0;

system ServerMovement {
    query = Query.all(PlayerTag, NetTransform, PlayerInput);

    on_server_update(delta: float) {
        foreach (var entity in query) {
            var trans = entity.get<NetTransform>();
            var input = entity.get<PlayerInput>();
            
            var move_dir = 0;
            if (input.move_left)  move_dir -= 1;
            if (input.move_right) move_dir += 1;
            trans.vx = move_dir * MOVE_SPEED;
            
            if (input.jump && is_on_ground(trans)) {
                trans.vy = JUMP_FORCE;
            }
            
            trans.vy += GRAVITY * delta;
            trans.x += trans.vx * delta;
            trans.y += trans.vy * delta;
            
            if (trans.y > 500) {
                trans.y = 500;
                trans.vy = 0;
            }
            
            var snapshot = snapshot_from(trans);
            NetworkManager.send_unreliable(
                BROADCAST_ALL, 
                MSG_SERVER_STATE, 
                snapshot
            );
        }
    }
}
```

### 客户端预测与和解

```tsx
system ClientPredictionMovement {
    query = Query.all(PlayerTag, NetTransform, PlayerInput, PredictedState);

    on_client_update(delta: float) {
        foreach (var entity in query) {
            var tag = entity.get<PlayerTag>();
            var trans = entity.get<NetTransform>();
            var input = entity.get<PlayerInput>();
            var pred = entity.get<PredictedState>();
            
            var pred_x = trans.x;
            var pred_y = trans.y;
            var pred_vx = trans.vx;
            var pred_vy = trans.vy;
            
            # 同样的移动逻辑...
            
            pred.predicted_x = pred_x;
            pred.predicted_y = pred_y;
            
            trans.x = pred_x;
            trans.y = pred_y;
        }
    }
    
    on_receive_server_state(msg: ServerStateMessage) {
        var entity = get_entity_by_player(msg.player_id);
        var trans = entity.get<NetTransform>();
        var pred = entity.get<PredictedState>();
        
        if (msg.frame < pred.server_frame) return;
        
        var error_x = msg.x - pred.predicted_x;
        var error_y = msg.y - pred.predicted_y;
        
        if (abs(error_x) > 0.1 || abs(error_y) > 0.1) {
            trans.x = msg.x;
            trans.y = msg.y;
            trans.vx = msg.vx;
            trans.vy = msg.vy;
        }
        
        pred.last_server_x = msg.x;
        pred.last_server_y = msg.y;
        pred.server_frame = msg.frame;
    }
}
```

## 帧同步战斗系统

帧同步要求所有客户端输入一致，在固定时间步长内执行确定性逻辑。

### 架构图

```
┌─────────────┐         ┌─────────────┐
│   客户端 A   │         │   客户端 B   │
│             │         │             │
│ ┌─────────┐ │  输入   │ ┌─────────┐ │
│ │输入收集  │ │ ─────→ │ │输入收集  │ │
│ └─────────┘ │         │ └─────────┘ │
│             │         │             │
│ ┌─────────┐ │  同步   │ ┌─────────┐ │
│ │确定性逻辑│ │ ←────→ │ │确定性逻辑│ │
│ └─────────┘ │         │ └─────────┘ │
│             │         │             │
│ ┌─────────┐ │  哈希   │ ┌─────────┐ │
│ │状态哈希  │ │ ←────→ │ │状态哈希  │ │
│ └─────────┘ │         │ └─────────┘ │
└─────────────┘         └─────────────┘
```

### 帧同步系统实现

```tsx
[Lockstep(tick_rate = 30)]
system LockstepCombat {
    query_players = Query.all(PlayerTag, CombatStats, NetTransform);
    query_attacks = Query.all(AttackEvent);

    on_lockstep_update(frame: int, inputs: map<int, PlayerInput>) {
        foreach (var player in query_players) {
            var tag = player.get<PlayerTag>();
            var input = inputs[tag.player_id];
            var stats = player.get<CombatStats>();
            
            if (stats.attack_cooldown > 0) {
                stats.attack_cooldown -= get_fixed_delta();
            }
            
            if (input.attack && stats.attack_cooldown <= 0) {
                var event_entity = create_entity();
                var evt = event_entity.add<AttackEvent>();
                evt.attacker_id = tag.player_id;
                evt.damage = stats.attack_damage;
                evt.frame = frame;
                
                stats.attack_cooldown = 0.5;
            }
        }
        
        foreach (var evt_entity in query_attacks) {
            var evt = evt_entity.get<AttackEvent>();
            
            var attacker = get_player(evt.attacker_id);
            var target = get_player(1 - evt.attacker_id);
            
            var attacker_trans = attacker.get<NetTransform>();
            var target_trans = target.get<NetTransform>();
            
            var dx = attacker_trans.x - target_trans.x;
            var dy = attacker_trans.y - target_trans.y;
            var dist = sqrt(dx*dx + dy*dy);
            
            if (dist < 50) {
                var target_stats = target.get<CombatStats>();
                target_stats.health -= evt.damage;
                log_info("Player " + evt.target_id + " took damage, health: " + target_stats.health);
            }
            
            destroy_entity(evt_entity);
        }
        
        var hash = calculate_state_hash();
        NetworkManager.send_reliable(BROADCAST_ALL, MSG_SYNC_HASH, hash);
    }
}
```

### 确定性要求

| 要求 | 描述 |
| :--- | :--- |
| 固定时间步长 | 所有客户端使用相同的 `tick_rate` |
| 输入一致性 | 所有客户端在相同帧收到相同输入 |
| 浮点确定性 | 避免浮点精度问题，使用定点数或确定性浮点库 |
| 随机数同步 | 所有客户端使用相同的随机种子 |

## 主场景与模式切换

### 模式切换 UI

```tsx
widget ModeSwitchPanel {
    property current_mode: SyncMode = SyncMode.None;

    enum SyncMode {
        None,
        StateSync,
        FrameSync
    }

    render() {
        <vbox>
            <text value="同步模式选择" font_size=24 />
            
            <hbox>
                <button text="单机" on_click=() => set_mode(SyncMode.None) />
                <button text="状态同步 (服务器权威)" on_click=() => set_mode(SyncMode.StateSync) />
                <button text="帧同步 (Lockstep)" on_click=() => set_mode(SyncMode.FrameSync) />
            </hbox>
            
            <text value="当前模式: " + current_mode.to_string() />
            
            <% if (current_mode == SyncMode.StateSync) { %>
                <checkbox label="作为服务器" bind=is_server />
                <input_field label="服务器地址" bind=server_address />
                <button text="连接" on_click=connect_state_sync />
            <% } else if (current_mode == SyncMode.FrameSync) { %>
                <button text="创建房间" on_click=create_lockstep_lobby />
                <button text="加入房间" on_click=join_lockstep_lobby />
            <% } %>
        </vbox>
    }

    function set_mode(mode: SyncMode) {
        current_mode = mode;
        game.reload_network_stack(mode);
    }

    function connect_state_sync() {
        NetworkManager.init();
        if (is_server) {
            NetworkManager.start_server(7777);
        } else {
            NetworkManager.connect_client(server_address, 7777);
        }
        game.enable_system("ServerMovement");
        game.enable_system("ClientPredictionMovement");
        game.disable_system("LockstepCombat");
    }

    function create_lockstep_lobby() {
        NetworkManager.init();
        NetworkManager.create_lobby();
        game.enable_system("LockstepCombat");
        game.disable_system("ServerMovement");
        game.set_fixed_timestep(1.0 / 30.0);
    }
}
```

### 主场景入口

场景数据以 gon 格式定义（`assets/scenes/game_main.scene`）：

```gon
Scene {
    name: "GameMain",
    entities: [],
    systems: [
        SceneSystem { type_name: "GameMainSystem", is_enabled: true }
    ],
    environment: {}
}
```

主场景逻辑由 `GameMainSystem` 驱动：

```tsx
system GameMainSystem {
    on_load() {
        let player1_entity = create_player(0);
        let player2_entity = create_player(1);
        
        ui_root.add_child(widget_mode_switch.create());
        ui_root.add_child(widget_health_bar.create(player1_entity));
        ui_root.add_child(widget_health_bar.create(player2_entity));
        
        game.set_sync_mode(SyncMode.None);
    }

    on_update(delta: f32) {
        let messages = NetworkManager.poll();
        loop msg in messages {
            dispatch_network_message(msg);
        }
        render_sprites();
    }
}

micro create_player(id: int): Entity {
    let e = create_entity();
    e.add<PlayerTag>({ player_id: id, local_device: id });
    e.add<NetTransform>({ x: 100 + id*200, y: 500 });
    e.add<CombatStats>();
    e.add<PredictedState>();
    return e;
}
```

## 运行方式与切换说明

| 模式 | 宏定义 | 后端 | 激活系统 | 权威方 |
| :--- | :--- | :--- | :--- | :--- |
| **单机** | `NET_BACKEND=NONE` | 无 | Input → 直接移动 | 本地 |
| **状态同步** | `NET_BACKEND=STEAM` 或 `WEBSOCKET` | Steam/WebSocket | ServerMovement + ClientPrediction | 服务器 |
| **帧同步** | `NET_BACKEND=STEAM` | Steam P2P | LockstepCombat | 输入由网络汇集，本地确定性执行 |

通过 UI 按钮切换模式时，游戏会重新初始化网络堆栈并启用对应的系统组。由于 gg 支持热重载，切换后立即生效。

## 关键同步机制对比

| 维度 | 状态同步 (移动) | 帧同步 (战斗) |
| :--- | :--- | :--- |
| **谁输入** | 客户端发送输入给服务器 | 客户端广播输入给所有玩家 |
| **谁权威** | 服务器计算位置 | 各客户端独立计算（输入一致则结果一致） |
| **谁响应** | 客户端立即预测，收到权威后和解 | 延迟固定帧数后执行，无预测 |
| **网络流量** | 高频位置快照 (可压缩) | 低频输入包 (每帧一个字节) |
| **延迟容忍** | 通过预测掩盖 | 必须等待最慢玩家 (Lockstep) |
| **作弊防护** | 服务器校验 | 需要额外机制 |
| **适用场景** | MMO、FPS、动作游戏 | RTS、格斗、回合制 |

## 最佳实践

### 状态同步

- 服务器是唯一真相源
- 客户端预测逻辑与服务器逻辑保持一致
- 使用差值压缩减少带宽

### 帧同步

- 确保逻辑确定性
- 使用状态哈希检测不同步
- 实现回滚机制处理延迟

## 下一步

- 阅读 [网络架构](../development/network.md) 了解详细设计
- 查看 [反作弊体系](../development/anti-cheat.md) 了解安全防护
