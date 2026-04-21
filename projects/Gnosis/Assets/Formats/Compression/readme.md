# 🗜️ Compression 压缩服务模块

## 📋 概述

Compression 模块提供资产压缩和解压缩服务，支持多种压缩算法，优化存储空间和加载性能。

## 🎯 支持的压缩算法

| 算法 | 压缩比 | 速度 | 适用场景 |
|:---|:---|:---|:---|
| LZ4 | 中等 | 极快 | 实时解压、运行时资产 |
| Zstd | 高 | 快 | 发布版本资产 |
| Deflate | 高 | 中等 | 通用压缩 |

## 🏗️ 核心组件

- **CompressionService** - 压缩服务主类，提供统一的压缩/解压接口
- **CompressionType** - 压缩类型枚举

## 🔧 使用示例

```csharp
# 压缩数据
var compressed = CompressionService.Compress(data, CompressionType.Zstd);

# 解压数据
var original = CompressionService.Decompress(compressed);
```
