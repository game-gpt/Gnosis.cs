# 📡 Events 事件系统模块

## 📋 概述

Events 模块提供领域事件的发布订阅机制，支持模块间的松耦合通信。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 事件定义 | 定义领域事件基类和接口 |
| 事件发布 | 支持事件的发布和广播 |
| 事件订阅 | 支持事件的订阅和处理 |

## 🏗️ 组件结构

```
Events/
├── IDomainEvent.cs            # 领域事件接口
├── DomainEventBase.cs         # 领域事件基类
├── IDomainEventHandler.cs     # 事件处理器接口
└── ConfigChangedEvent.cs      # 配置变更事件
```

## 🔧 使用示例

```csharp
# 定义事件
public class PlayerJoinedEvent : DomainEventBase
{
    public PlayerId PlayerId { get; set; }
}

# 订阅事件
public class PlayerHandler : IDomainEventHandler<PlayerJoinedEvent>
{
    public void Handle(PlayerJoinedEvent evt)
    {
        # 处理玩家加入逻辑
    }
}
```

## 🔗 相关模块

- [Core](../) - 核心模块
- [ECS](../../ECS) - ECS 系统
