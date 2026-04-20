# 多阶段编程模型与整体架构

本文档介绍 Gnosis 引擎的核心设计理念：多阶段编程（MSP）范式，以及引擎的整体架构。

## 设计哲学

传统游戏开发面临以下困境：

| 问题 | 传统方案 | gg 引擎方案 |
| :--- | :--- | :--- |
| 引擎绑定特定图形 API | 硬编码依赖 | RHI 抽象层 |
| 热更新依赖 JIT | iOS/主机限制 | 字节码解释 |
| 网络同步模型僵化 | 单一模式 | 帧同步/状态同步融合 |
| 反作弊能力薄弱 | 客户端校验 | 服务器权威 |

gg 引擎通过多阶段编程范式解决这些问题。

## 多阶段编程模型

gg 引擎将构建过程划分为六个阶段，每个阶段的输出成为下一阶段的输入：

```
负二阶段 → 负一阶段 → 阶段〇 → 阶段一 → 阶段二 → 阶段三
```

### 阶段划分

| 阶段 | 名称 | 输入 | 输出 | 职责 |
| :--- | :--- | :--- | :--- | :--- |
| 负二阶段 | 插件配置 | gg 插件 | 宏表、能力注册表 | 加载插件，注入宏定义 |
| 负一阶段 | 资产预处理 | 原始资产 | 平台特化资产 | 格式转换、压缩优化 |
| 阶段〇 | 开发编辑 | gg 源码 | 编辑状态 | 编辑器交互、实时预览 |
| 阶段一 | 元语言执行 | gg 源码 + 宏 | 字节码 + VM 源码 | 编译、生成虚拟机 |
| 阶段二 | AOT 编译 | VM 源码 | 本地二进制 | 编译为可执行文件 |
| 阶段三 | 打包发布 | 字节码 + 资产 + 二进制 | 最终游戏包 | 打包、签名、分发 |

### 阶段详细说明

#### 负二阶段：插件配置

插件在负二阶段加载，用于向编译环境注入平台能力与宏定义：

```tsx
plugin WeChatChannel {
    requires_arch = ["WASM"];
    provides_macros = ["WECHAT", "WECHAT_SHARE"];
    provides_capabilities = ["WeChatLogin", "WeChatShare"];

    export function login(): Promise<UserInfo> {
        // 绑定至微信 SDK
    }
}
```

C# 元语言加载插件：

```csharp
public class PluginLoader
{
    public void load_plugin(string gg_plugin_path, ChannelMacros global_macros)
    {
        var ast = parse_gg(gg_plugin_path);
        if (!ast.requires_arch.Contains(current_arch)) return;
        
        global_macros.add_range(ast.provides_macros);
        
        foreach (var cap in ast.provides_capabilities)
            CapabilityRegistry.register(cap, ast.name);
    }
}
```

#### 负一阶段：资产预处理

原始资产（.psd、.fbx、.csv）被转换为平台优化的引擎格式：

```csharp
public class AssetPipeline
{
    public void import_texture(string path, PlatformProfile profile)
    {
        var raw = load_image(path);
        foreach (var arch in profile.target_archs)
        {
            var compressed = compress_for_arch(raw, arch);
            save_as_engine_asset(compressed, arch, "texture");
        }
    }
}
```

#### 阶段一：元语言执行

gg 编译器将 gg 源码翻译为平台无关字节码，同时生成特化虚拟机 C 源码：

```csharp
public class GGCompiler
{
    public CompilationResult compile(
        string[] source_files,
        ArchTarget arch,
        ChannelMacros macros,
        bool is_editor_build = false)
    {
        var ast = parse_gg_sources(source_files);
        
        var meta_evaluator = new MetaLanguageEvaluator(macros);
        var processed_ast = meta_evaluator.process(ast);
        
        var bytecode_gen = new BytecodeGenerator(arch, is_editor_build);
        var bytecode = bytecode_gen.generate(processed_ast);
        
        var vm_src_gen = new VMSourceGenerator(arch, bytecode_gen.used_features);
        var vm_sources = vm_src_gen.generate();
        
        return new CompilationResult(bytecode, vm_sources);
    }
}
```

#### 阶段二：AOT 编译

生成的虚拟机是纯 C 代码，可经任何 C 编译器生成 iOS、主机平台所需的本机二进制：

```c
void vm_run(VMState* vm) {
    const uint8_t* ip = vm->ip;
    static void* dispatch[] = { &&OP_HALT, &&OP_ADD_F, &&OP_CALL_NATIVE, ... };
    goto *dispatch[*ip++];
    
OP_ADD_F: {
    float b = vm->stack[--vm->sp];
    float a = vm->stack[--vm->sp];
    vm->stack[vm->sp++] = a + b;
    goto *dispatch[*ip++];
}

OP_CALL_NATIVE: {
    uint32_t func_id = *(uint32_t*)ip; ip += 4;
    native_functions[func_id](vm);
    goto *dispatch[*ip++];
}
}
```

**关键优势**：
- **AOT 兼容**：虚拟机为纯 C 代码，无 JIT 限制
- **零反射**：所有类型信息在构建时解析完毕

### 数据流图

```mermaid
flowchart LR
    subgraph 负二阶段["负二阶段：插件配置"]
        plugins["gg 插件"] --> macro_table["全局宏表"]
        plugins --> cap_reg["能力注册表"]
    end

    subgraph 负一阶段["负一阶段：资产预处理"]
        raw["原始资产"] --> importer["资产导入器"]
        importer --> cooked_assets["平台特化资产"]
    end

    subgraph 阶段〇["阶段〇：开发编辑"]
        editor["编辑器 (gg)"] <--> src["gg 源码"]
        editor <--> cooked_assets
    end

    subgraph 阶段一["阶段一：元语言执行"]
        src --> compiler["gg_compiler"]
        macro_table --> compiler
        compiler --> bytecode["gg 字节码 (.ggc)"]
        compiler --> vm_src["虚拟机源码 (C)"]
    end

    subgraph 阶段二["阶段二：AOT 编译"]
        vm_src --> aot["AOT 编译器"]
        aot --> kernel["运行时内核 (.exe/.wasm)"]
    end

    subgraph 阶段三["阶段三：打包发布"]
        bytecode --> packer["资产打包器"]
        cooked_assets --> packer
        kernel --> packer
        packer --> final["最终游戏包"]
    end

    负二阶段 --> 阶段〇
    负一阶段 --> 阶段〇
    阶段〇 --> 阶段一
    阶段一 --> 阶段二
    阶段二 --> 阶段三
```

## 整体架构

### 架构层次

```
┌─────────────────────────────────────────────────────────────┐
│                     游戏逻辑层 (gg 字节码)                     │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              ECS 世界 (组件 + 系统)                    │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                     引擎核心层 (C AOT 内核)                    │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐                 │
│  │ 虚拟机    │   │ ECS 核心  │   │ 资源管理  │                │
│  └──────────┘   └──────────┘   └──────────┘                 │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                     平台抽象层                                │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐                 │
│  │ RHI      │   │ 网络后端  │   │ 平台插件  │                │
│  └──────────┘   └──────────┘   └──────────┘                 │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                     原生平台层                                │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐                 │
│  │ Vulkan   │   │ Metal    │   │ DirectX  │                │
│  └──────────┘   └──────────┘   └──────────┘                 │
└─────────────────────────────────────────────────────────────┘
```

### 核心模块

| 模块 | 描述 |
| :--- | :--- |
| `gg_compiler` | gg 语言编译器，生成字节码 |
| `vm_generator` | 虚拟机源码生成器 |
| `asset_pipeline` | 资产预处理管线 |
| `gg_vm` | 字节码解释器 |
| `ecs_runtime` | ECS 运行时 |
| `rhi` | 渲染硬件接口抽象层 |

## ECS 架构

gg 引擎采用实体组件系统（ECS）架构管理游戏对象：

### 核心概念

| 概念 | 描述 |
| :--- | :--- |
| Entity | 实体，仅是一个唯一标识符 |
| Component | 组件，纯数据，无逻辑 |
| System | 系统，纯逻辑，查询并处理组件 |
| World | 世界，包含所有实体和组件的容器 |
| Archetype | 原型，具有相同组件组合的实体集合 |

### 查询机制

```tsx
system MoveSystem {
    query = Query.all(Position, Velocity);

    on_update(delta: float) {
        <% foreach (var (pos, vel) in query) { %>
            pos.x += vel.vx * delta;
            pos.y += vel.vy * delta;
        <% } %>
    }
}
```

## 插件系统

插件在负二阶段加载，用于注入平台能力和宏定义：

```tsx
plugin WeChatChannel {
    requires_arch = ["WASM"];
    provides_macros = ["WECHAT", "WECHAT_SHARE"];
    provides_capabilities = ["WeChatLogin", "WeChatShare"];
}
```

## 热更新机制

### 热重载（开发期）

文件变更时：
1. 增量编译新 `.ggc` 模块
2. 通知虚拟机替换内存中的模块定义
3. ECS 世界自动使用新定义创建后续实体

### 热更新（发布后）

游戏启动时：
1. 检查更新清单
2. 下载增量 `.ggc` 字节码与资产差分文件
3. 通过 VFS 覆盖旧版本

## 设计原则

| 原则 | 描述 |
| :--- | :--- |
| 渲染不可知论 | 引擎内核不绑定任何特定图形 API |
| 服务器为唯一真相源 | 联网游戏的逻辑边界严格控制在服务端 |
| 软失败与可观测性 | 防御采用静默降级、延迟惩罚策略 |
| 可插拔架构 | 网络后端、渲染后端、平台插件均可替换 |

## 下一步

- 阅读 [gg 语言指南](gg-language.md) 学习 ECS 编程
- 阅读 [网络架构](network.md) 了解帧同步与状态同步
- 阅读 [渲染系统](rendering.md) 了解 RHI 抽象层
