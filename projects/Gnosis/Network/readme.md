# 🌐 Network 网络模块

## 📋 概述

Network 模块提供多人游戏的网络通信支持，包括服务器权威架构、帧同步、状态同步和预测回滚等核心功能。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 网络连接 | 管理客户端-服务器连接 |
| 消息序列化 | 高效的二进制消息序列化 |
| 帧同步 | 确定性帧同步支持 |
| 状态同步 | 服务器权威状态同步 |
| 预测回滚 | 客户端预测和服务器回滚 |
| 多后端支持 | Steam、WebSocket 等网络后端 |

## 🏗️ 模块结构

```
Network/
├── INetworkManager.cs       # 网络管理器接口
├── NetworkManager.cs        # 网络管理器实现
├── INetworkBackend.cs       # 网络后端接口
├── NetworkBackendBase.cs    # 网络后端基类
├── SteamNetworkBackend.cs   # Steam 网络后端
├── WebSocketBackend.cs      # WebSocket 后端
├── NullNetworkBackend.cs    # 空网络后端（单机）
├── INetworkMessage.cs       # 网络消息接口
├── NetworkMessage.cs        # 网络消息实现
├── IMessageSerializer.cs    # 消息序列化接口
├── MessageSerializer.cs     # 消息序列化实现
├── ILockstepSystem.cs       # 帧同步系统接口
├── LockstepSystem.cs        # 帧同步系统实现
├── IStateSyncSystem.cs      # 状态同步系统接口
├── StateSyncSystem.cs       # 状态同步系统实现
├── PredictionSystem.cs      # 预测系统
├── DeterministicExecutor.cs # 确定性执行器
├── NetworkMode.cs           # 网络模式
├── ConnectionState.cs       # 连接状态
└── ReplicatedAttribute.cs   # 复制属性标记
```

## 🔄 网络架构

```
┌─────────────────────────────────────────────┐
│              服务器（权威）                    │
│  ┌─────────────────────────────────────┐    │
│  │         StateSyncSystem             │    │
│  │  验证输入 → 更新状态 → 广播结果       │    │
│  └─────────────────────────────────────┘    │
└─────────────────────────────────────────────┘
              ↓ ↑ 网络消息
┌─────────────────────────────────────────────┐
│              客户端                          │
│  ┌─────────────────────────────────────┐    │
│  │       PredictionSystem              │    │
│  │  本地预测 → 显示 → 收到服务器确认     │    │
│  │       ↓                             │    │
│  │  回滚（如需要）→ 重新预测             │    │
│  └─────────────────────────────────────┘    │
└─────────────────────────────────────────────┘
```

## 📊 网络模式

| 模式 | 描述 |
|:---|:---|
| `Offline` | 单机离线模式 |
| `Host` | 主机模式（服务器+客户端） |
| `Client` | 纯客户端模式 |
| `DedicatedServer` | 专用服务器模式 |

## 🔌 网络后端

| 后端 | 描述 |
|:---|:---|
| `SteamNetworkBackend` | Steam P2P 网络 |
| `WebSocketBackend` | WebSocket 连接 |
| `NullNetworkBackend` | 单机模拟 |

## 🔗 相关模块

- [Security](../Security) - 反作弊验证
- [ECS](../ECS) - 状态同步组件
