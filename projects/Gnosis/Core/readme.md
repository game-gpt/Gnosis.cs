# 🎯 Core 核心模块

## 📋 概述

Core 模块是 gg 引擎的基础设施层，提供引擎各模块共用的核心类型、接口和基础服务。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 基础类型 | 定义引擎核心数据类型（EntityId、PlayerId 等） |
| 组件接口 | 定义 ECS 组件和系统的基础接口 |
| 配置系统 | 管理引擎和游戏配置 |
| 时间管理 | 提供统一的时间服务 |
| 事件系统 | 领域事件发布订阅机制 |

## 🏗️ 模块结构

```
Core/
├── Events/           # 领域事件系统
├── IComponent.cs     # 组件接口
├── ISystem.cs        # 系统接口
├── IEntity.cs        # 实体接口
├── EntityId.cs       # 实体 ID 类型
├── PlayerId.cs       # 玩家 ID 类型
├── Position.cs       # 位置类型
├── ConfigSystem.cs   # 配置系统
├── TimeManager.cs    # 时间管理器
├── Timestamp.cs      # 时间戳类型
└── BuildStage.cs     # 构建阶段枚举
```

## 🔧 核心类型

### 🆔 标识类型

| 类型 | 描述 |
|:---|:---|
| `EntityId` | 实体唯一标识符 |
| `PlayerId` | 玩家唯一标识符 |
| `RegionId` | 区域唯一标识符 |

### ⚙️ 枚举类型

| 类型 | 描述 |
|:---|:---|
| `BuildStage` | 构建阶段（负一阶段、运行时等） |
| `PlatformType` | 目标平台类型 |

## 🔗 相关模块

- [ECS](../ECS) - 实体组件系统
- [Events](./Events) - 事件系统
