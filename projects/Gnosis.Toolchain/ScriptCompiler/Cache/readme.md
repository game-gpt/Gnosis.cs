# 💾 Cache 编译缓存模块

## 📋 概述

Cache 模块提供编译结果缓存，支持增量编译，大幅减少重复编译时间。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 缓存管理 | 存储和检索编译结果 |
| 增量编译 | 仅重新编译变更的文件 |
| 缓存失效 | 检测依赖变更自动失效 |

## 🏗️ 组件结构

```
Cache/
├── ICompilationCache.cs       # 缓存接口
├── InMemoryCompilationCache.cs # 内存缓存实现
├── FileCompilationCache.cs    # 文件缓存实现
└── CacheKeyGenerator.cs       # 缓存键生成器
```

## 🔧 缓存策略

| 策略 | 实现 | 适用场景 |
|:---|:---|:---|
| 内存缓存 | `InMemoryCompilationCache` | 开发时快速迭代 |
| 文件缓存 | `FileCompilationCache` | 持久化编译结果 |

## 🔗 相关模块

- [Compiler](../) - 使用缓存加速编译
