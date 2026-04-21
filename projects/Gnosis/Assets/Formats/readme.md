# 📄 Formats 格式处理模块

## 📋 概述

Formats 模块提供各类游戏资产格式的解析和序列化支持，采用插件化架构，支持自定义格式扩展。

## 🎯 支持的格式类型

| 格式类型 | 接口 | 描述 |
|:---|:---|:---|
| 🎨 纹理 | `ITextureFormat` | PNG、JPEG、KTX、Basis 等图片格式 |
| 🎭 网格 | `IMeshFormat` | OBJ、GLTF、FBX 等模型格式 |
| 🎬 动画 | `IAnimationFormat` | 骨骼动画、变形动画格式 |
| 🔊 音频 | `IAudioFormat` | WAV、MP3、OGG 等音频格式 |
| 🎨 材质 | `IMaterialFormat` | PBR 材质定义格式 |
| 🏠 预制体 | `IPrefabFormat` | 实体预制体格式 |
| 🗺️ 场景 | `ISceneFormat` | 场景描述格式 |
| 🌐 本地化 | `ILocalizationFormat` | 多语言配置格式 |
| ⚙️ 配置 | `IConfigFormat` | JSON、TOML、GON 配置格式 |

## 🏗️ 架构设计

```
FormatRegistry (格式注册表)
       │
       ├── FormatHandlerBase (格式处理器基类)
       │       │
       │       ├── TextureFormatHandler
       │       ├── MeshFormatHandler
       │       ├── AudioFormatHandler
       │       └── ...
       │
       └── IFileIO (文件 I/O 抽象)
               │
               └── PhysicalFileIO
```

## 🔧 使用示例

```csharp
# 注册自定义格式处理器
var registry = new FormatRegistry();
registry.RegisterHandler(new CustomFormatHandler());

# 加载资产
var texture = await registry.LoadAsync<ITexture>("textures/hero.png");
var mesh = await registry.LoadAsync<IMesh>("models/character.gltf");
```

## 🔗 相关模块

- [Assets](../) - 资产管理主模块
- [Compression](./Compression) - 资产压缩服务
