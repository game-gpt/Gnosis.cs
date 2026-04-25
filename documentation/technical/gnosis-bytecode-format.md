# .gnosis 字节码模块格式规范 v2.0

## 概述

`.gnosis` 文件是 Gnosis VM 的字节码模块格式，由 Gnosis 编译器工具链产出，由 Gnosis Runtime 加载执行。

Gnosis VM 是 Nyar 元编译器框架产出的游戏特化虚拟机，基于 Game 方言。与通用虚拟机（Nyar VM，基于 Standard 方言）不同，Gnosis VM 的字节码格式针对游戏场景做了深度特化——不仅是"加了几个游戏相关的指令"，而是从值表示、指令语义、模块结构到热更新机制的全栈游戏优化。

## 设计原则

- **高度特化**：字节码格式为 Game 方言量身定制，不做通用化妥协。特化不是锦上添花，而是核心设计驱动力
- **单一格式**：所有 `.gnosis` 文件使用统一的 GNOS 魔数和二进制布局，不存在变体
- **栈式执行**：采用栈式字节码模型，指令简洁紧凑，适合实时解释执行
- **热更新友好**：模块结构天然支持原子替换，符号表和依赖声明使运行时可以精确判断兼容性

## Game 方言特化设计

### 为什么特化？特化了什么？

通用虚拟机（如 Nyar VM）面向通用计算，其指令集是类型化的算术运算（i32.add、f64.mul 等），值表示是简单的联合体。Gnosis VM 面向游戏，游戏有独特的计算模式：

| 游戏计算模式 | 通用 VM 的处理方式 | Gnosis VM 的特化方式 |
|:---|:---|:---|
| 实体-组件操作 | 函数调用 + 对象反射 | **ECS 专用指令**（0x80-0x8E），直接操作 World |
| 实体标识 | 整数 ID + 运行时检查 | **NaN-Boxing Entity 类型**，48 位载荷存储 Index+Generation |
| 帧循环调度 | 回调/事件系统 | **WorldUpdate 指令**，内置系统调度和拓扑排序 |
| 协程/异步 | 通用代数效应 | **Yield/Resume 指令**，与热重载深度集成 |
| 热重载 | 无原生支持 | **模块元数据**（导入/导出/依赖），支持兼容性比对和状态迁移 |

### 特化带来的优化

1. **零开销实体操作**：`SpawnEntity` 指令直接调用 `_world.CreateEntity()`，无需经过函数调用协议、参数装箱、反射查找。一条指令 = 一次 ECS World 调用，中间零抽象层

2. **NaN-Boxing 值表示**：GGValue 利用 IEEE 754 双精度浮点数的 NaN 编码空间存储类型标签，Entity 类型直接编码在 64 位值中（`EntityTag | Generation<<32 | Index`），实体比较和传递无需解引用

3. **批量操作优化**：GnosisDialect 的等价重写规则（如 `gnosis-entity-batch` 将连续实体创建合并为批量创建、`gnosis-query-merge` 将独立查询合并为批量查询）在编译期将多个 ECS 指令序列优化为更高效的形式

4. **帧预算感知**：GnosisDialect 的成本模型以游戏帧预算为约束（ECS 查询 50 周期、AI 规划 200 周期、寻路 150 周期），优化器在等价意图中选择帧预算内的最低成本实现

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

### 为什么没有函数表？

`.gnosis` 使用扁平指令流而非函数表设计。原因：

- **热重载友好**：扁平指令流中，函数入口是常量池索引 + 跳转偏移，替换模块时只需重映射索引，无需重建函数表
- **跨模块调用统一**：`CallModule` 指令通过模块名 + 符号名调用，不依赖函数表索引，模块替换后调用点无需修改
- **编译简单**：BytecodeGenerator 直接输出线性指令序列，无需额外的函数布局规划

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

### 指令 vs 函数：为什么 ECS 操作是指令而不是函数调用？

这是一个核心设计决策。将 ECS 操作设计为指令而非 `CallNative` 函数调用，原因远不止"省一次函数分派"：

**1. 语义保证**

`CallNative` 是动态分派——运行时根据 `funcId` 查表调用，编译器无法知道调用目标的行为。而 ECS 指令的语义是固定的：`SpawnEntity` 一定创建实体并返回 Entity 值，`GetComponent` 一定从 World 中读取组件。编译器可以基于此做等价重写优化（如查询合并、批量实体创建），而函数调用无法做这种优化，因为编译器不知道函数的副作用。

**2. 值类型协同**

ECS 指令与 GGValue 的 Entity 类型深度协同。`SpawnEntity` 返回的 `GGValue.FromEntity(entityId)` 将 Index+Generation 直接编码在 NaN-Boxing 的 48 位载荷中，后续 `GetComponent`、`HasComponent` 等指令直接解码载荷，无需间接寻址。如果用函数调用，返回值需要经过通用的函数返回协议（可能涉及堆分配），Entity 的零开销表示就失效了。

**3. 帧预算控制**

游戏每帧有严格的预算（16.6ms/60fps）。ECS 指令的成本是可预测的（GnosisDialect 成本模型：ECS 查询 50 周期），编译器可以在等价意图中选择帧预算内的最低成本实现。而 `CallNative` 的成本是不透明的，编译器无法对函数调用做帧预算感知的优化。

**4. 调度集成**

`WorldUpdate` 指令不是"调用一个更新函数"，而是触发整个 ECS 调度管线——按 `SystemPhase` 分阶段、按依赖关系拓扑排序、执行所有注册的脚本系统。这是 VM 级别的调度语义，不是函数调用能表达的。

**5. 热重载兼容性**

ECS 指令的操作数是常量池索引和类型索引，模块替换时 `MetadataComparer` 可以精确比对类型索引的兼容性。如果 ECS 操作是函数调用，兼容性比对只能检查函数签名，无法深入到 ECS 语义层面。

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

### ECS 指令详解

ECS 指令（0x80-0x8E）是 Gnosis VM 区别于通用 VM 的核心，每条指令直接映射到 ECS World 操作：

| 指令 | 操作数 | 栈效果 | 语义 |
|:---|:---|:---|:---|
| SpawnEntity | 无 | → Entity | 创建实体，返回 EntityId（Index+Generation） |
| DestroyEntity | Entity | → | 销毁实体，释放组件 |
| AddComponent | Entity, typeIdx, data | → | 为实体添加类型索引对应的组件 |
| GetComponent | Entity, typeIdx | → Value | 读取实体组件数据 |
| SetComponent | Entity, typeIdx, data | → | 写入实体组件数据 |
| RemoveComponent | Entity, typeIdx | → | 移除实体组件 |
| HasComponent | Entity, typeIdx | → Bool | 检查实体是否拥有组件 |
| QueryAll | typeIdx[] | → Entity[] | 查询拥有所有指定类型的实体（交集） |
| QueryAny | typeIdx[] | → Entity[] | 查询拥有任一指定类型的实体（并集） |
| QueryWith | typeIdx | → Entity[] | 单类型查询 |
| QueryWithout | typeIdx | → Entity[] | 排除类型查询 |
| DefineComponent | name, schema | → typeIdx | 注册脚本组件类型，返回类型索引 |
| DefineSystem | name, phase, funcAddr | → | 注册脚本系统（名称+阶段+函数地址） |
| SystemSchedule | system, dependency | → | 声明系统间依赖关系 |
| WorldUpdate | dt | → | 执行 ECS 调度管线（按阶段+拓扑排序执行所有系统） |

**ComponentTypeRegistry** 桥接字节码类型索引与运行时组件存储，支持两种模式：
- C# 原生组件：通过 `ComponentPool<T>` 存储，零开销
- 脚本组件：通过 `ScriptComponentStorage` 存储，使用 `GGStruct` 表示数据

**ScriptSystemScheduler** 按 `SystemPhase` 分阶段调度，支持依赖声明和拓扑排序，确保系统执行顺序的正确性。

### 协程指令详解

| 指令 | 操作数 | 栈效果 | 语义 |
|:---|:---|:---|:---|
| Yield | value | → | 挂起当前协程，保存栈帧快照，将 value 传递给调用者 |
| Resume | coroutine, value | → result | 恢复协程执行，传递 value，返回协程下一次 Yield 的值或最终结果 |

协程指令与热重载深度集成：Yield 保存的栈帧快照可以在热重载后由 `StateMigrator` 迁移到新模块的栈帧布局。

### 操作数编码

操作数采用小端序，宽度由 OpCode 决定：

| OpCode | 操作数宽度 |
|:---|:---|
| PushInt8 | 1 字节 |
| PushInt16 | 2 字节 |
| PushInt32, LoadLocal, StoreLocal 等 | 4 字节 |
| PushInt64, PushFloat64, CallModule | 8 字节 |
| 无操作数指令（算术/逻辑/Return 等） | 0 字节 |

### CallNative 与内置函数

`CallNative`（0x51）用于调用由宿主环境注册的原生函数。与 ECS 指令不同，原生函数是**动态注册的**，编译时无法确定其行为，因此无法做等价重写优化。

**原生函数 vs 指令的边界**：

| 能力 | 实现方式 | 原因 |
|:---|:---|:---|
| ECS 操作 | 指令（0x80-0x8E） | 语义固定，可优化，与 Entity 值类型协同 |
| 协程控制 | 指令（0xD0-0xD1） | 与栈帧管理深度耦合，热重载需要栈帧快照 |
| 数学函数 | CallNative | 语义简单，无需编译器感知，宿主环境可能提供硬件加速 |
| I/O 操作 | CallNative | 平台相关，语义不固定 |
| 字符串操作 | 指令（0x90-0x93） | 高频操作，内联避免函数调用开销 |
| 渲染提交 | CallNative | 后端相关（Vulkan/Metal/DX12），语义不固定 |

**常用内置函数**（通过 `CallNative` 调用，由 `NativeFunctionRegistry` 管理）：

| 类别 | 函数 | 描述 |
|:---|:---|:---|
| 数学 | `math.sin`, `math.cos`, `math.sqrt`, `math.abs`, `math.rand` | 三角函数、随机数 |
| 时间 | `time.now`, `time.delta` | 帧时间、系统时间 |
| 调试 | `debug.log`, `debug.assert` | 日志输出、断言 |
| 渲染 | `render.draw_sprite`, `render.set_camera` | 精灵绘制、相机控制 |
| 物理 | `physics.raycast`, `physics.overlap` | 射线检测、重叠检测 |
| 网络 | `net.rpc`, `net.sync` | 远程调用、状态同步 |
| 音频 | `audio.play`, `audio.stop` | 音效播放 |

这些函数不是指令，因为它们的语义要么是平台相关的（渲染、物理、网络），要么是足够简单以至于编译器不需要感知其内部行为（数学函数）。将它们设计为原生函数而非指令，使得宿主环境可以灵活替换实现（如切换渲染后端），而无需修改字节码格式。

## 热重载与热更新

### 两种模式

| 模式 | 场景 | 触发方式 | 机制 |
|:---|:---|:---|:---|
| **热重载** | 开发期 | 文件变更自动触发 | 模块替换 + 状态迁移 |
| **热更新** | 发布后 | CDN 推送 / 客户端拉取 | 字节码下载 + 模块加载 |

### 字节码对热更新的设计支持

`.gnosis` 格式的多个设计决策直接服务于热更新：

**1. 模块名称 + 依赖声明**

模块名称和依赖列表使运行时可以精确判断替换影响范围。当模块 A 被替换时，运行时通过依赖图找到所有依赖 A 的模块，判断是否需要级联重载。

**2. 导入/导出符号表**

符号表使 `MetadataComparer` 可以精确比对模块接口的兼容性：
- 新增导出符号 → 兼容，直接加载
- 删除导出符号 → 不兼容，依赖方需要级联重载
- 修改符号签名 → 不兼容，需要迁移映射

**3. 扁平指令流**

扁平指令流中，函数入口是常量池索引 + 跳转偏移。模块替换时只需重映射常量池索引，无需重建函数表。跨模块调用通过 `CallModule`（模块名 + 符号名）实现，模块替换后调用点无需修改——符号名是稳定的接口契约。

**4. 常量池索引**

所有字符串、类型引用通过常量池索引间接访问。模块替换时，新旧模块的常量池可以独立管理，`StateMigrator` 只需映射索引即可迁移状态。

### 热重载流程（开发期）

```
文件变更 → 重新编译 → MetadataComparer 比对差异
    ↓
兼容？
    ├── 是 → 创建状态快照 → 卸载旧模块 → 加载新模块 → StateMigrator 迁移状态 → 继续
    └── 否 → 拒绝重载 / 全量重启
```

**状态迁移策略**：

| 策略 | 描述 | 适用场景 |
|:---|:---|:---|
| Preserve | 保留原值 | 同名同类型字段 |
| Reset | 重置为 Null | 新增字段 |
| Convert | 类型转换 | int → float 等兼容类型变更 |
| Default | 默认行为 | 无法自动判断时 |

**协程栈帧迁移**：Yield 保存的栈帧快照包含 IP、局部变量、调用帧。热重载时 `StateMigrator` 将旧栈帧的局部变量映射到新栈帧布局，IP 重定向到新模块的对应指令位置。

### 热更新流程（发布后）

```
CDN 推送版本清单 → 客户端比对本地版本 → 下载差异包
    → 验证哈希 → 加载新模块 → 替换旧模块 → 继续
```

**合规性**：Gnosis VM 是 AOT 编译的本地代码，下载的 `.gnosis` 文件是数据（由 VM 解释执行），不涉及可执行代码的动态生成（无 JIT），等价于下载新的关卡数据或脚本配置，符合 iOS App Store 和各主机平台政策。

**差异更新策略**：

| 策略 | 描述 | 适用场景 |
|:---|:---|:---|
| 全量替换 | 下载完整 `.gnosis` 模块 | 小模块或首版 |
| 二进制差异 | 下载 bsdiff 补丁，客户端还原 | 大模块小幅更新 |
| 模块级差异 | 仅下载变更的模块 | 多模块项目部分更新 |

## GGValue：NaN-Boxing 值表示

Gnosis VM 使用 NaN-Boxing 混合值类型，利用 IEEE 754 双精度浮点数的 NaN 编码空间存储类型标签：

| 标签（高 16 位） | 类型 | 载荷（低 48 位） | 描述 |
|:---|:---|:---|:---|
| 0x7FF8 | Float | IEEE 754 位模式 | 规范化 NaN |
| 0x7FF9 | Int | 有符号整数 | 支持 48 位范围 |
| 0x7FFA | Bool | 0 或 1 | — |
| 0x7FFB | Null | — | — |
| 0x7FFC | **Entity** | **Generation(16) + Index(32)** | **游戏特化：实体 ID 直接编码在值中** |
| 0x7FFD | Ref | — | GC 引用类型（对象存储在 _reference） |
| 0x7FFE | NativeRef | — | 非 GC 原生引用 |

**Entity 类型的 NaN-Boxing 编码**是游戏特化的关键：实体 ID 的 Index（32 位）和 Generation（16 位）直接打包在 48 位载荷中，实体比较是整数比较（`_bits == other._bits`），实体传递是值传递（无需堆分配），实体在栈上的存储与 int/float 一样高效。

## 相关格式

| 格式 | 魔数 | 用途 |
|:---|:---|:---|
| `.gnosis` | `0x474E4F53` ("GNOS") | Gnosis VM 字节码模块 |
| `.gnosis.asm` | 文本格式 | Gnosis VM 反汇编文本 |
| `.gnosis-debug` | `0x47474449` ("GGDI") | Gnosis VM 调试信息 |

### 关于模块打包

`.gnosis` 已经是一个完整的、自包含的游戏模块——包含常量池、符号表、依赖声明和全部指令。单个 `.gnosis` 文件可以被 Gnosis Runtime 直接加载执行，无需任何额外的打包格式。

实际项目中，一个游戏由多个 `.gnosis` 模块组成（如 `player.gnosis`、`enemy.gnosis`、`ui.gnosis`），它们通过 `CallModule` 指令和导入/导出符号表互相引用。分发时，`ModulePackager` 将多个 `.gnosis` 文件打包为 `.gnosis-bundle`（魔数 `0x47474D42` "GGMB"），这是一个**分发格式**而非执行格式：

- 打包支持压缩（LZ4）和加密（AES-256-CBC）
- 包含模块清单（`module_manifest.gon`），声明模块间依赖
- 运行时通过 VFS 挂载点按需加载，无需解压到磁盘
- 打包是可选的——开发期直接加载散落的 `.gnosis` 文件，发布期打包为 `.gnosis-bundle`

`.gnosis-bundle` 的详细格式由 `Gnosis.Asset` 包的 `AssetBundler` 定义，不属于本规范范围。

## 与 Nyar VM 的关系

Gnosis VM 和 Nyar VM 是 Nyar 元编译器框架产出的两个平级虚拟机，各自针对其方言特化：

| 维度 | Gnosis VM | Nyar VM |
|:---|:---|:---|
| 方言 | Game | Standard |
| 字节码 | `.gnosis`（GNOS 魔数） | `.nyar`（NYAR 魔数） |
| ECS | 专用指令（0x80-0x8E） | 通过 `BuiltinCall` 分派 |
| 值表示 | NaN-Boxing（含 Entity 类型） | 简单联合体 |
| 热重载 | ModuleReloader + StateMigrator | ModuleHotReloader + 依赖图 |
| 定位 | 游戏场景专用 | 通用计算 |

两者互不兼容，字节码不能交叉执行。如需其他场景的虚拟机，应定义新的方言和对应的字节码格式，而非复用现有格式——这是"高度特化以达到高度优化"的核心思想。

## 历史变更

| 版本 | 日期 | 变更 |
|:---|:---|:---|
| v1.0 | 2026-04-25 | 初始规范。统一 GGBC 和 GNOS 为单一 GNOS 格式，消除双魔数问题 |
| v1.1 | 2026-04-25 | 修正字符串编码为 LEB128（兼容 BinaryWriter.Write），修正浮点常量为 f32 |
| v2.0 | 2026-04-25 | 新增 Game 方言特化设计说明、指令 vs 函数设计决策、ECS 指令详解、热重载/热更新机制、GGValue NaN-Boxing 说明、模块打包定位澄清 |

## ⛔ 已废弃

| 格式 | 魔数 | 状态 | 说明 |
|:---|:---|:---|:---|
| GGBC | `0x47474243` | **已废弃** | 原 ScriptCompiler BytecodeGenerator 使用的格式，已合并到 GNOS |
