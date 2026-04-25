# .gnosis 字节码模块格式规范 v1.0

## 概述

`.gnosis` 文件是 Gnosis VM 的字节码模块格式，由 Gnosis 编译器工具链产出，由 Gnosis Runtime 加载执行。

Gnosis VM 是基于 Game 方言的高度特化游戏虚拟机，其字节码格式针对游戏场景优化，内建 ECS 指令集。

## 设计原则

- **高度特化**：字节码格式为 Game 方言量身定制，不做通用化妥协
- **单一格式**：所有 `.gnosis` 文件使用统一的 GNOS 魔数和二进制布局，不存在变体
- **栈式执行**：采用栈式字节码模型，指令简洁紧凑，适合实时解释执行

## 文件结构

```
+----------------------+
| Magic (4 bytes)      | 0x47 0x4E 0x4F 0x53 ("GNOS")
+----------------------+
| Version (2 bytes)    | u16 = 1
+----------------------+
| Module Name          | length(u16) + UTF-8 bytes
+----------------------+
| Constant Pool        | count(i32) + entries[]
+----------------------+
| Imported Symbols     | count(u16) + Leb128String[]
+----------------------+
| Exported Symbols     | count(u16) + Leb128String[]
+----------------------+
| Dependencies         | count(u16) + Leb128String[]
+----------------------+
| Instructions         | length(i32) + bytes[]
+----------------------+
```

## 字段详细说明

### 文件头

| 偏移 | 大小 | 字段 | 描述 |
|:---|:---|:---|:---|
| 0x00 | 4 | Magic | 魔数 `0x474E4F53`（"GNOS"，小端序存储） |
| 0x04 | 2 | Version | 版本号，当前为 `1` |

### 模块名称

| 字段 | 类型 | 描述 |
|:---|:---|:---|
| length | u16 | 模块名称 UTF-8 字节长度 |
| name | byte[length] | UTF-8 编码的模块名称 |

> ⚠️ 模块名称使用 **u16 长度前缀**，与符号表的 LEB128 长度前缀不同。

### 常量池

| 字段 | 类型 | 描述 |
|:---|:---|:---|
| count | i32 | 常量条目数量 |
| entries | ConstantEntry[count] | 常量条目序列 |

每个 ConstantEntry：

| 字段 | 类型 | 描述 |
|:---|:---|:---|
| tag | u8 | 常量类型标签 |
| data | 变长 | 常量数据（由 tag 决定格式） |

常量类型标签：

| tag | 类型 | data 格式 |
|:---|:---|:---|
| 0x01 | String | LEB128 长度 + UTF-8 bytes |
| 0x02 | Int | value(i32, 小端序) |
| 0x03 | Float | value(f32, 小端序) |

> ⚠️ 字符串常量使用 **LEB128 长度前缀**（兼容 .NET `BinaryWriter.Write(string)` 的 7-bit 编码格式），而非固定宽度 i32。浮点常量为 **f32（4 字节）**，而非 f64。

### 导入符号表

| 字段 | 类型 | 描述 |
|:---|:---|:---|
| count | u16 | 导入符号数量 |
| entries | Leb128String[count] | 符号名称序列 |

### 导出符号表

| 字段 | 类型 | 描述 |
|:---|:---|:---|
| count | u16 | 导出符号数量 |
| entries | Leb128String[count] | 符号名称序列 |

### 依赖列表

| 字段 | 类型 | 描述 |
|:---|:---|:---|
| count | u16 | 依赖模块数量 |
| entries | Leb128String[count] | 模块名称序列 |

> ⚠️ 符号表和依赖列表中的字符串均使用 **LEB128 长度前缀**（兼容 .NET `BinaryWriter.Write(string)` 的 7-bit 编码格式）。

### 指令段

| 字段 | 类型 | 描述 |
|:---|:---|:---|
| length | i32 | 指令字节码长度 |
| bytes | byte[length] | 原始指令字节码 |

### 字符串编码说明

本格式中存在两种字符串编码方式，均由 `Gnosis.Toolchain.BytecodeGenerator` 的实现决定：

| 位置 | 长度前缀 | 编码 | 来源 |
|:---|:---|:---|:---|
| 模块名称 | u16（2 字节固定） | UTF-8 | `writer.Write((ushort)nameBytes.Length)` |
| 常量字符串 | LEB128（变长） | UTF-8 | `writer.Write(string)` → 7-bit 编码长度 + UTF-8 |
| 符号表字符串 | LEB128（变长） | UTF-8 | `writer.Write(string)` → 7-bit 编码长度 + UTF-8 |

LEB128 长度前缀的编码规则（.NET `BinaryWriter.Write(string)` 使用的 7-bit 编码）：

- 每个字节的低 7 位为数据位，最高位为续位标志
- 最高位为 1 表示后续还有字节，为 0 表示结束
- 小端序排列（最低有效组在前）
- 对于长度 < 128 的字符串，仅需 1 字节前缀

## 指令集

Gnosis VM 指令集为 Game 方言特化，操作码为单字节（`byte` 范围）。

### 指令集布局

| 范围 | 类别 | 操作码 |
|:---|:---|:---|
| 0x00-0x01 | 控制 | Halt=0x00, Nop=0x01 |
| 0x10-0x18 | 常量推送 | PushInt8=0x10, PushInt16=0x11, PushInt32=0x12, PushInt64=0x13, PushFloat32=0x14, PushFloat64=0x15, PushTrue=0x16, PushFalse=0x17, PushNull=0x18 |
| 0x20-0x21 | 栈操作 | Pop=0x20, Dup=0x21 |
| 0x30-0x4B | 算术/比较/逻辑 | AddInt=0x30 ~ Not=0x4B |
| 0x50-0x53 | 调用 | Call=0x50, CallNative=0x51, Return=0x52, CallModule=0x53 |
| 0x60-0x65 | 变量访问 | LoadLocal=0x60, StoreLocal=0x61, LoadGlobal=0x62, StoreGlobal=0x63, LoadField=0x64, StoreField=0x65 |
| 0x70-0x72 | 对象 | NewObject=0x70, GetField=0x71, SetField=0x72 |
| 0x80-0x8E | ECS（Game 方言特化） | SpawnEntity=0x80, DestroyEntity=0x81, AddComponent=0x82, GetComponent=0x83, RemoveComponent=0x84, QueryAll=0x85, QueryAny=0x86, DefineComponent=0x87, DefineSystem=0x88, SetComponent=0x89, HasComponent=0x8A, QueryWith=0x8B, QueryWithout=0x8C, SystemSchedule=0x8D, WorldUpdate=0x8E |
| 0x90-0x93 | 字符串 | PushString=0x90, ConcatString=0x91, StringLength=0x92, StringGetChar=0x93 |
| 0xA0-0xA3 | 数组 | NewArray=0xA0, ArrayGet=0xA1, ArraySet=0xA2, ArrayLength=0xA3 |
| 0xB0-0xB2 | 闭包 | MakeClosure=0xB0, GetUpvalue=0xB1, SetUpvalue=0xB2 |
| 0xC0-0xC2 | 类型检查 | IsNull=0xC0, IsType=0xC1, TypeOf=0xC2 |
| 0xD0-0xD1 | 协程 | Yield=0xD0, Resume=0xD1 |

### 操作数编码

操作数采用小端序，宽度由 OpCode 决定：

| OpCode | 操作数宽度 |
|:---|:---|
| PushInt8 | 1 字节 |
| PushInt16 | 2 字节 |
| PushInt32, LoadLocal, StoreLocal 等 | 4 字节 |
| PushInt64, PushFloat64, CallModule | 8 字节 |
| 无操作数指令（算术/逻辑/Return 等） | 0 字节 |

## 相关格式

| 格式 | 魔数 | 用途 |
|:---|:---|:---|
| `.gnosis` | `0x474E4F53` ("GNOS") | Gnosis VM 字节码模块 |
| `.gnosis.asm` | 文本格式 | Gnosis VM 反汇编文本 |
| `.gnosis-bundle` | `0x47474D42` ("GGMB") | Gnosis VM 模块打包文件 |
| `.gnosis-debug` | `0x47474449` ("GGDI") | Gnosis VM 调试信息 |

## 历史变更

| 版本 | 日期 | 变更 |
|:---|:---|:---|
| v1.0 | 2026-04-25 | 初始规范。统一 GGBC 和 GNOS 为单一 GNOS 格式，消除双魔数问题 |
| v1.1 | 2026-04-25 | 修正字符串编码为 LEB128（兼容 BinaryWriter.Write），修正浮点常量为 f32 |

## ⛔ 已废弃

| 格式 | 魔数 | 状态 | 说明 |
|:---|:---|:---|:---|
| GGBC | `0x47474243` | **已废弃** | 原 ScriptCompiler BytecodeGenerator 使用的格式，已合并到 GNOS |
