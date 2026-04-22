# C# 编码规范

本文档定义 Gnosis 项目的 C# 编码规范，适用于 Layer 1（元引擎）和 Layer 2（游戏引擎）的所有 C# 代码。

***

## 通用原则

1. **一致性**：同一项目内风格必须统一
2. **可读性**：代码是写给人看的，其次才是给机器执行的
3. **简洁性**：选择最简单、最清晰的实现方式
4. **安全性**：不暴露敏感信息，不引入安全隐患

***

## 命名规范

### 大小写规则

| 标识符      | 风格             | 示例                                    |
| -------- | -------------- | ------------------------------------- |
| 命名空间     | PascalCase     | `Gnosis.ECS`                          |
| 类、结构体、接口 | PascalCase     | `ArchetypeStorage`, `IQueryFilter`    |
| 枚举类型     | PascalCase     | `ShaderStage`                         |
| 枚举值      | PascalCase     | `ShaderStage.Vertex`                  |
| 方法       | PascalCase     | `CreateEntity()`, `GetComponent<T>()` |
| 属性       | PascalCase     | `EntityCount`, `IsAlive`              |
| 字段（私有）   | \_camelCase    | `_chunkSize`, `_entityCount`          |
| 字段（公有）   | PascalCase     | `MaxEntities`, `DefaultCapacity`      |
| 常量       | PascalCase     | `MaxChunkCapacity`                    |
| 局部变量     | camelCase      | `entityId`, `deltaTime`               |
| 参数       | camelCase      | `delta`, `archetypeId`                |
| 类型参数     | T + PascalCase | `TComponent`, `TEntity`               |
| 布尔变量/属性  | is/has/can 前缀  | `IsAlive`, `HasComponent`, `CanSpawn` |

### 命名空间规范

命名空间遵循 `Gnosis.{包名}.{子模块}` 的模式：

```csharp
namespace Gnosis.ECS.Core;
namespace Gnosis.ECS.Implementation;
namespace Gnosis.Graphic.RHI;
namespace Gnosis.Network.Transport;
```

### 包命名铁律

| 规则            | 正确示例                | 错误示例                    |
| :------------ | :------------------ | :---------------------- |
| 包名使用**单数名词**  | `Gnosis.Asset`      | `Gnosis.Assets`         |
| 子模块不使用主包名     | `Graphic.FX`        | `Graphic.Core`          |
| 避免动词或 -ing 形式 | `Animation.Tween`   | `Animation.Tweening`    |
| 适配器模式命名后缀     | `Animation.Adapter` | `Animation.SpinePlugin` |

***

## 代码结构

### 文件组织

一个文件应只包含一个主要类型，文件名与类型名一致：

```
ArchetypeStorage.cs    → class ArchetypeStorage
IQueryFilter.cs        → interface IQueryFilter
EntityId.cs            → struct EntityId
```

### 成员排列顺序

```csharp
public class ExampleClass
{
    #region 常量

    public const int MaxCapacity = 1024;

    #endregion

    #region 静态字段

    private static int _instanceCount;

    #endregion

    #region 实例字段

    private readonly int _id;
    private string _name;

    #endregion

    #region 构造函数

    public ExampleClass(int id)
    {
        _id = id;
    }

    #endregion

    #region 属性

    public int Id => _id;

    public string Name
    {
        get => _name;
        set => _name = value;
    }

    #endregion

    #region 公有方法

    public void Process()
    {
    }

    #endregion

    #region 私有方法

    private void InternalProcess()
    {
    }

    #endregion
}
```

### Region 使用规则

- 大文件（超过 200 行）**必须**使用 `#region` 划分代码区域
- Region 名称使用中文
- 常用的 Region 名称：常量、字段、构造函数、属性、公有方法、私有方法

***

## 注释规范

### 注释语言

- 所有注释必须使用**中文**
- XML 文档注释也应使用中文

### 注释位置

- 注释必须放在代码**上方**，禁止使用后置注释（行尾注释）
- 建议使用 XML 文档注释

### XML 文档注释

```csharp
/// <summary>
/// 创建一个新的实体并返回其标识符
/// </summary>
/// <param name="archetypeId">原型标识符</param>
/// <returns>新创建的实体标识符</returns>
public EntityId CreateEntity(int archetypeId)
{
}
```

### 空行规则

- 方法之间保留一个空行
- 逻辑块之间保留一个空行
- 注释上方保留一个空行（除非是文件开头或紧跟大括号）

***

## 语句规范

### 大括号使用

- `if`、`for`、`foreach`、`while`、`do`、`switch` 等语句后面**必须**使用大括号
- 即使只有一行代码，也不能省略大括号
- 大括号独占一行（Allman 风格）

```csharp
if (condition)
{
    DoSomething();
}

for (int i = 0; i < count; i++)
{
    Process(i);
}
```

### 字符串使用

- 字符串拼接优先使用字符串插值 `$""`
- 多行字符串使用 `@""`

```csharp
var message = $"用户 {userName} 创建成功";

var sql = @"
    SELECT *
    FROM Users
    WHERE IsActive = 1
";
```

### 异常处理

- 不捕获通用异常 `Exception`，应捕获具体异常
- 异常消息应使用中文描述

```csharp
try
{
    await SaveUserAsync(user);
}
catch (DbUpdateException ex)
{
    _logger.LogError(ex, "保存用户失败：{UserId}", user.Id);
    throw;
}
```

### using 声明

- 使用 `using` 声明替代 `using` 块（当不需要精确控制释放时机时）
- 多个 using 按长度从短到长排列

```csharp
using var stream = File.OpenRead(path);
using var reader = new StreamReader(stream);
```

### null 检查

- 使用模式匹配进行 null 检查
- 使用 `??` 和 `?.` 运算符

```csharp
if (entity is null)
{
    return;
}

var name = entity?.Name ?? "Unknown";
```

***

## 性能相关规范

### Span 与 Memory

- 处理连续内存时优先使用 `Span<T>` 和 `ReadOnlySpan<T>`
- 避免不必要的数组分配

```csharp
public void Process(ReadOnlySpan<byte> data)
{
    foreach (var b in data)
    {
    }
}
```

### 对象池

- 频繁创建/销毁的对象使用对象池
- `Gnosis.Core.Collection` 提供了对象池实现

### 避免装箱

- 值类型传递时使用泛型约束而非 `object`
- 使用 `IEquatable<T>` 而非 `object.Equals`

### ref 结构体

- 仅在栈上使用的类型标记为 `ref struct`
- 避免将 `ref struct` 传递到异步上下文

***

## 包依赖规范

### 依赖方向

依赖方向必须遵循：**基础层 ← 核心层 ← 子系统层**，绝不允许反向依赖。

```
基础层:     Gnosis.Core, Gnosis.IR
               ↑
核心层:     Gnosis.Runtime, Gnosis.ECS, Gnosis.Asset, ...
               ↑
子系统层:   Gnosis.Graphic, Gnosis.Network, Gnosis.Physics, ...
```

### 依赖检查

1. 这个包是否**必须**依赖另一个包？能否通过接口解耦？
2. 依赖方向是否与架构层级一致？
3. 是否引入了**循环依赖**？
4. 这个包将来是否会被**可选地替换**？如果是，请引入 `Provider` / `Adapter` 接口。

***

## 扩展点模式

| 后缀         | 使用场景               | 示例                  |
| :--------- | :----------------- | :------------------ |
| `Adapter`  | 将第三方数据格式或运行时接入现有系统 | `IAnimationAdapter` |
| `Provider` | 提供数据存储或配置的后端实现     | `IStorageProvider`  |
| `Driver`   | 硬件或底层库的抽象驱动        | `IAudioDriver`      |

### 接口定义

```csharp
public interface IAudioDriver
{
    string Name { get; }
    void Initialize();
    void Shutdown();
}
```

### 实现命名

```csharp
public sealed class OpenALDriver : IAudioDriver
{
    public string Name => "OpenAL";
}
```

***

## 测试规范

### 测试命名

```
{方法名}_{场景}_{预期结果}

示例：
CreateEntity_WithValidArchetype_ReturnsEntityId
Process_WhenQueueIsEmpty_DoesNothing
```

### 测试结构 (AAA)

```csharp
[Fact]
public void CreateEntity_WithValidArchetype_ReturnsEntityId()
{
    // Arrange
    var world = new World();
    var archetypeId = world.RegisterArchetype<Position, Velocity>();

    // Act
    var entityId = world.CreateEntity(archetypeId);

    // Assert
    Assert.True(entityId.IsValid);
}
```

***

## 禁止事项

| 禁止                     | 原因             |
| ---------------------- | -------------- |
| 使用 `System.Reflection` | 运行时反射违反多阶段编程原则 |
| 使用 `dynamic` 类型        | 破坏类型安全         |
| 使用 `unsafe` 代码（未经审核）   | 安全风险           |
| 在引擎代码中使用 `async void`  | 异常无法捕获         |
| 硬编码文件路径                | 跨平台兼容性         |
| 提交密钥或凭据                | 安全风险           |

