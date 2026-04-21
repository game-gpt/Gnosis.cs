# 🧩 ECS 实体组件系统模块

## 📋 概述

ECS（Entity-Component-System）模块是 gg 引擎的核心架构模式，采用数据驱动设计，实现逻辑与数据的分离，支持高效的数据布局和并行处理。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 实体管理 | 管理游戏世界中的所有实体 |
| 组件存储 | 高效的组件数据存储和访问 |
| 系统调度 | 按依赖关系调度系统执行 |
| 查询优化 | 高效的实体查询机制 |

## 🏗️ 模块结构

```
ECS/
├── IWorld.cs             # 世界接口
├── IArchetype.cs         # 原型接口
├── IQuery.cs             # 查询接口
├── ISystemGroup.cs       # 系统组接口
├── ISystemScheduler.cs   # 系统调度器接口
├── ComponentPool.cs      # 组件池
├── ComponentStorage.cs   # 组件存储
├── ComponentAccess.cs    # 组件访问控制
├── ComponentSerializer.cs # 组件序列化
├── QueryMode.cs          # 查询模式枚举
└── SystemPhase.cs        # 系统执行阶段
```

## 🔄 ECS 架构

```
┌─────────────────────────────────────────┐
│              World (世界)                 │
│  ┌─────────────────────────────────┐    │
│  │         Archetype (原型)         │    │
│  │  ┌─────────┐ ┌─────────┐        │    │
│  │  │ Entity  │ │ Entity  │ ...    │    │
│  │  │ ┌─────┐ │ │ ┌─────┐ │        │    │
│  │  │ │CompA│ │ │ │CompA│ │        │    │
│  │  │ │CompB│ │ │ │CompB│ │        │    │
│  │  │ └─────┘ │ │ └─────┘ │        │    │
│  │  └─────────┘ └─────────┘        │    │
│  └─────────────────────────────────┘    │
└─────────────────────────────────────────┘
         ↓ Query
┌─────────────────────────────────────────┐
│           System (系统)                   │
│  query all(Position, Velocity)          │
│  on_update(dt) { ... }                  │
└─────────────────────────────────────────┘
```

## 📊 查询模式

| 模式 | 语法 | 描述 |
|:---|:---|:---|
| All | `query all(A, B)` | 拥有所有指定组件的实体 |
| Any | `query any(A, B)` | 拥有任意指定组件的实体 |
| None | `query none(A)` | 不拥有指定组件的实体 |

## 🎯 系统执行阶段

| 阶段 | 描述 |
|:---|:---|
| `OnInit` | 初始化阶段 |
| `OnLoad` | 加载阶段 |
| `OnUpdate` | 每帧更新 |
| `OnFixedUpdate` | 固定时间步更新 |
| `OnLateUpdate` | 延迟更新 |
| `OnDestroy` | 销毁阶段 |

## 🔗 相关模块

- [Core](../Core) - 基础接口定义
- [Compiler](../Compiler) - ECS 组件和系统的编译
