# Gnosis 引擎

## 项目名称

Gnosis 引擎

## 描述

采用多阶段编程（MSP）范式的跨平台游戏引擎，包含两个主要项目：

- **Gnosis** - 引擎内核，提供编译器、虚拟机、ECS 运行时等核心功能
- **GnosisEngine** - 渲染层，提供 RHI 抽象、着色器系统、渲染管线、Widget 等所有涉及渲染的部分

## 项目定位

### Gnosis（引擎内核）
- 作为引擎内核使用，不包含任何渲染相关代码
- 包含编译器、虚拟机、ECS 运行时、网络同步、反作弊等核心组件
- 遵循"渲染不可知论"原则，引擎内核不绑定任何特定图形 API

### GnosisEngine（渲染层）
- 承载所有涉及渲染的部分，包括 RHI、着色器、管线、Widget
- 基于"一切皆 Shader"理念，以 `IMicroFunction` 为核心抽象统一所有渲染范式
- 支持光栅化、光线追踪、神经渲染、扩散模型等多种渲染后端

## 核心特性

- **gg 语言编译器** - 支持多阶段编程的游戏专用语言编译器
- **字节码虚拟机** - 高性能字节码执行引擎，支持热重载
- **ECS 运行时** - 实体组件系统架构，优化游戏逻辑组织
- **一切皆 Shader** - `micro` 函数作为 GPU 可编程管线的通用最小执行单元
- **RHI 抽象层** - 可插拔的渲染后端（Vulkan、Metal、DirectX）
- **网络同步** - 客户端-服务器架构，支持状态同步和帧同步
- **反作弊防御** - 多层次反作弊系统，保护游戏公平性

## 架构概览

### Gnosis 内核结构

```
Gnosis/
├── Core/           # 核心抽象和基础类型
├── Compiler/       # gg 语言编译器
├── Interpreter/    # 字节码虚拟机
├── ECS/            # 实体组件系统
├── Formats/        # 资产格式处理
├── Network/        # 网络同步模块
├── Security/       # 反作弊系统
└── Infrastructure/ # 基础设施实现
```

### GnosisEngine 渲染层结构

```
GnosisEngine/
├── RHI/            # 渲染硬件接口抽象层
│   └── Enums/      # RHI 枚举类型
├── Shader/         # 着色器系统（一切皆 Shader）
│   └── ValueObjects/ # 着色器值对象
├── Pipeline/       # 渲染管线与帧图
│   └── ValueObjects/ # 管线值对象
├── Widget/         # Widget 渲染接口
├── Renderer/       # 渲染器实现
├── UI/             # UI 渲染与布局
├── Editor/         # 编辑器窗口
└── Program.cs      # 入口
```

## 许可证

MIT License
