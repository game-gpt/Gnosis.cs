# 🔒 Security 安全模块

## 📋 概述

Security 模块提供游戏反作弊和安全防护功能，支持单机游戏和联网游戏的分级防御体系。

## 🎯 核心职责

| 职责 | 描述 |
|:---|:---|
| 内存保护 | 加密敏感数据，防止内存修改 |
| 完整性校验 | 检测代码和数据的完整性 |
| 蜜罐陷阱 | 设置诱饵检测作弊行为 |
| 速率限制 | 限制函数调用频率 |
| 服务器验证 | 服务器权威验证 |

## 🏗️ 模块结构

```
Security/
├── IAntiCheatSystem.cs      # 反作弊系统接口
├── AntiCheatSystem.cs       # 反作弊系统实现
├── AntiCheatLevel.cs        # 反作弊等级
├── DefenseLevel.cs          # 防御等级
├── DetectionLevel.cs        # 检测等级
├── IMemoryProtector.cs      # 内存保护接口
├── MemoryEncryptor.cs       # 内存加密器
├── IEncryptedField.cs       # 加密字段接口
├── EncryptedField.cs        # 加密字段实现
├── IIntegrityChecker.cs     # 完整性检查接口
├── IntegrityChecker.cs      # 完整性检查实现
├── IHoneypot.cs             # 蜜罐接口
├── IRateLimiter.cs          # 速率限制接口
├── IServerValidator.cs      # 服务器验证接口
├── DebugDetector.cs         # 调试检测器
├── SaveProtector.cs         # 存档保护
├── EncryptedAttribute.cs    # 加密属性标记
├── HoneypotAttribute.cs     # 蜜罐属性标记
├── SensitiveAttribute.cs    # 敏感属性标记
├── ServerOnlyAttribute.cs   # 仅服务器属性
├── RateLimitAttribute.cs    # 速率限制属性
├── ResponseAction.cs        # 响应动作
├── ViolationType.cs         # 违规类型
├── ViolationResponse.cs     # 违规响应
└── SecurityException.cs     # 安全异常
```

## 📊 防御等级

| 等级 | 适用场景 | 核心能力 |
|:---|:---|:---|
| 基础级 | 独立开发者 | 变量加密、存档保护 |
| 增强级 | 中型团队 | 蜜罐陷阱、时间校验 |
| 专业级 | 大型项目 | 服务器校验、行为分析 |
| 企业级 | 头部网游 | 机器学习、法律溯源 |

## 🛡️ 防护能力

| 能力 | 描述 |
|:---|:---|
| `integrity_check` | 代码完整性校验 |
| `memory_obfuscate` | 内存混淆加密 |
| `archive_bind` | 存档硬件绑定 |
| `honeypot_field` | 蜜罐变量陷阱 |
| `server_authority` | 服务器权威验证 |
| `rate_limit` | 调用频率限制 |

## 🔗 相关模块

- [Network](../Network) - 网络验证
- [Core](../Core) - 基础类型
