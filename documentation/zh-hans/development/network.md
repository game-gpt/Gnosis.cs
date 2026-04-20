# 网络架构：帧同步与状态同步

本文档介绍 gg 引擎的网络架构，包括帧同步（Lockstep）与状态同步（Server Authority）的设计与实现。

## 设计理念

gg 引擎提供统一的网络抽象层，支持在同一游戏中无缝切换帧同步与状态同步：

| 特性 | 描述 |
| :--- | :--- |
| 后端可插拔 | 支持 Steam P2P、WebSocket 等 |
| 编译时选择 | 通过宏选择网络后端 |
| 模式切换 | 运行时可切换同步模式 |
| 服务器权威 | 联网游戏以服务器为唯一真相源 |

## 网络后端抽象

### 后端类型

| 后端 | 平台 | 特性 |
| :--- | :--- | :--- |
| Steam | PC (Steam) | P2P 可靠/不可靠传输 |
| WebSocket | Web / 专用服务器 | 客户端-服务器模式 |
| None | 单机 | 无网络 |

### 编译时选择

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
```

### 网络管理器

```tsx
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
        backend.send_reliable(target, packet);
    }

    static function send_unreliable(target: int, msg_id: int, data: byte[]) {
        var packet = pack_message(msg_id, data);
        backend.send_unreliable(target, packet);
    }

    static function poll(): Message[] {
        var raw = backend.receive();
        return parse_messages(raw);
    }
}
```

## 状态同步

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
system ServerMovement {
    query = Query.all(PlayerTag, NetTransform, PlayerInput);

    on_server_update(delta: float) {
        foreach (var entity in query) {
            var trans = entity.get<NetTransform>();
            var input = entity.get<PlayerInput>();
            
            // 权威移动计算
            trans.vx = input.move_dir * MOVE_SPEED;
            trans.x += trans.vx * delta;
            trans.y += trans.vy * delta;
            
            // 广播状态
            var snapshot = snapshot_from(trans);
            NetworkManager.send_unreliable(BROADCAST_ALL, MSG_SERVER_STATE, snapshot);
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
            // 本地预测移动（与服务器逻辑一致）
            var trans = entity.get<NetTransform>();
            var input = entity.get<PlayerInput>();
            var pred = entity.get<PredictedState>();
            
            // 预测逻辑
            trans.x += input.move_dir * MOVE_SPEED * delta;
            pred.predicted_x = trans.x;
        }
    }
    
    on_receive_server_state(msg: ServerStateMessage) {
        var entity = get_entity_by_player(msg.player_id);
        var trans = entity.get<NetTransform>();
        var pred = entity.get<PredictedState>();
        
        // 计算误差
        var error = abs(msg.x - pred.predicted_x);
        
        if (error > 0.1) {
            // 有显著误差，执行和解
            trans.x = msg.x;
            trans.y = msg.y;
        }
    }
}
```

## 帧同步

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

### 帧同步系统

```tsx
[Lockstep(tick_rate = 30)]
system LockstepCombat {
    query_players = Query.all(PlayerTag, CombatStats, NetTransform);

    on_lockstep_update(frame: int, inputs: map<int, PlayerInput>) {
        // 所有客户端执行相同的确定性逻辑
        foreach (var player in query_players) {
            var tag = player.get<PlayerTag>();
            var input = inputs[tag.player_id];
            var stats = player.get<CombatStats>();
            
            // 确定性战斗逻辑
            if (input.attack && stats.attack_cooldown <= 0) {
                // 创建攻击事件
                create_attack_event(tag.player_id, stats.attack_damage);
                stats.attack_cooldown = 0.5;
            }
        }
        
        // 发送同步哈希用于调试
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

## 模式切换

通过 UI Widget 可在运行时切换同步模式，引擎自动启用/禁用对应系统组并重设网络堆栈。

### 模式切换表格

| 模式 | 宏定义 | 后端 | 激活系统 | 权威方 |
|:---|:---|:---|:---|:---|
| 单机 | `NET_BACKEND=NONE` | 无 | Input → 直接移动 | 本地 |
| 状态同步 | `STEAM` 或 `WEBSOCKET` | Steam/WebSocket | ServerMovement + ClientPrediction | 服务器 |
| 帧同步 | `STEAM` | Steam P2P | LockstepCombat | 输入汇集，本地执行 |

### 模式切换 Widget 示例

```tsx
widget ModeSwitchPanel {
    property current_mode: SyncMode = SyncMode.None;

    enum SyncMode {
        None,       // 单机
        StateSync,  // 状态同步
        FrameSync   // 帧同步
    }

    render() {
        <panel title="网络模式">
            <button 
                text="单机模式" 
                active={current_mode == SyncMode.None}
                on_click={() => set_mode(SyncMode.None)}
            />
            <button 
                text="状态同步" 
                active={current_mode == SyncMode.StateSync}
                on_click={() => set_mode(SyncMode.StateSync)}
            />
            <button 
                text="帧同步" 
                active={current_mode == SyncMode.FrameSync}
                on_click={() => set_mode(SyncMode.FrameSync)}
            />
        </panel>
    }

    function set_mode(mode: SyncMode) {
        current_mode = mode;
        game.reload_network_stack(mode);
    }

    function connect_state_sync() {
        NetworkManager.init();
        game.enable_system("ServerMovement");
        game.enable_system("ClientPredictionMovement");
        game.disable_system("LockstepCombat");
    }

    function create_lockstep_lobby() {
        NetworkManager.init();
        game.enable_system("LockstepCombat");
        game.disable_system("ServerMovement");
        game.set_fixed_timestep(1.0 / 30.0);
    }
}
```

## 模式对比

| 维度 | 状态同步 | 帧同步 |
| :--- | :--- | :--- |
| 权威方 | 服务器 | 各客户端 |
| 网络流量 | 高频位置快照 | 低频输入包 |
| 延迟容忍 | 通过预测掩盖 | 必须等待最慢玩家 |
| 作弊防护 | 服务器校验 | 需要额外机制 |
| 适用场景 | MMO、FPS | RTS、格斗 |

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

- 阅读 [反作弊体系](anti-cheat.md) 了解安全防护
- 查看 [示例项目](../../examples/) 了解实际用法
