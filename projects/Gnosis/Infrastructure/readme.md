# 🏗️ Infrastructure 基础设施模块

## 📋 概述

Infrastructure 模块提供引擎运行所需的基础设施服务，包括日志、事件存储、仓储、虚拟文件系统等。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 日志服务 | 提供统一的日志记录接口 |
| 事件存储 | 持久化领域事件 |
| 仓储模式 | 数据访问抽象层 |
| 虚拟文件系统 | 跨平台文件访问抽象 |

## 🏗️ 模块结构

```
Infrastructure/
├── ILogger.cs              # 日志接口
├── ConsoleLogger.cs        # 控制台日志实现
├── LogLevel.cs             # 日志级别
├── IEventStore.cs          # 事件存储接口
├── InMemoryEventStore.cs   # 内存事件存储
├── IRepository.cs          # 仓储接口
└── IVirtualFileSystem.cs   # 虚拟文件系统接口
```

## 📊 日志级别

| 级别 | 图标 | 描述 |
|:---|:---|:---|
| Trace | 🔍 | 详细跟踪信息 |
| Debug | 🐛 | 调试信息 |
| Info | ℹ️ | 一般信息 |
| Warning | ⚠️ | 警告信息 |
| Error | ❌ | 错误信息 |
| Fatal | 💀 | 致命错误 |

## 🔗 相关模块

- [Core](../Core) - 核心类型
- [Network](../Network) - 网络基础设施
