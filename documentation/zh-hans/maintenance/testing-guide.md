# 测试指南

本文档定义 Gnosis 项目的测试策略与规范，适用于 Layer 1（元引擎）和 Layer 2（游戏引擎）的所有 C# 代码。

---

## 测试框架

| 组件 | 选择 | 说明 |
|------|------|------|
| 测试框架 | xUnit | .NET 生态主流测试框架 |
| 断言库 | xUnit Assert | 内置断言 |
| Mock 框架 | Moq | 接口 Mock |
| 覆盖率 | coverlet | 跨平台覆盖率收集 |

---

## 项目结构

```
projects/
├── Gnosis/                    # 源码
│   ├── ECS/
│   ├── Network/
│   └── ...
└── Gnosis.Tests/              # 测试项目
    ├── ECS/
    │   ├── EntityTests.cs
    │   ├── ArchetypeTests.cs
    │   └── QueryTests.cs
    ├── Network/
    │   ├── TransportTests.cs
    │   └── SyncTests.cs
    └── ...
```

---

## 测试分类

### 单元测试

测试单个类或方法的行为，不依赖外部资源。

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

### 参数化测试

使用 `[Theory]` 和 `[InlineData]` 测试多组输入：

```csharp
[Theory]
[InlineData(0, 0, 0)]
[InlineData(1, 0, 1)]
[InlineData(0, 1, 1)]
[InlineData(3, 4, 7)]
public void Add_TwoNumbers_ReturnsSum(int a, int b, int expected)
{
    var result = a + b;
    Assert.Equal(expected, result);
}
```

### 集成测试

测试多个组件的协作：

```csharp
[Fact]
public async Task NetworkSync_ClientServer_StateConsistent()
{
    // Arrange
    var server = new NetworkHost();
    var client = new NetworkClient();
    await server.Start(9900);
    await client.Connect("localhost", 9900);

    // Act
    server.SetState(new GameState { Score = 100 });
    await Task.Delay(100);

    // Assert
    var clientState = client.GetState<GameState>();
    Assert.Equal(100, clientState.Score);
}
```

---

## 运行测试

### 基本命令

```bash
# 运行所有测试
dotnet test

# 运行特定项目
dotnet test projects/Gnosis.Tests

# 运行特定测试
dotnet test --filter "FullyQualifiedName~EntityTests"

# 详细输出
dotnet test --logger "console;verbosity=detailed"
```

### 覆盖率

```bash
# 收集覆盖率
dotnet test --collect:"XPlat Code Coverage"

# 生成报告
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report
```

### 持续集成

所有 PR 必须通过完整的测试套件：

```bash
dotnet test --configuration Release
```

---

## 测试命名规范

### 格式

```
{方法名}_{场景}_{预期结果}
```

### 示例

| 测试名 | 描述 |
|--------|------|
| `CreateEntity_WithValidArchetype_ReturnsEntityId` | 正常创建实体 |
| `CreateEntity_WithInvalidArchetype_ThrowsException` | 无效原型抛异常 |
| `DestroyEntity_WhenEntityAlive_MarksAsDead` | 销毁活着的实体 |
| `Query_AllMatchingEntities_ReturnsAll` | 查询所有匹配实体 |

---

## 测试结构 (AAA)

每个测试遵循 Arrange-Act-Assert 模式：

```csharp
[Fact]
public void SetComponent_UpdatesValue()
{
    // Arrange
    var world = new World();
    var entity = world.CreateEntity<Position>();
    ref var pos = ref world.GetComponent<Position>(entity);

    // Act
    pos.X = 100;

    // Assert
    Assert.Equal(100, world.GetComponent<Position>(entity).X);
}
```

---

## Mock 使用

### 接口 Mock

```csharp
[Fact]
public void LoadAsset_WithValidPath_CallsProvider()
{
    // Arrange
    var mockProvider = new Mock<IStorageProvider>();
    mockProvider
        .Setup(p => p.Load(It.IsAny<string>()))
        .Returns(new AssetData());

    var assetManager = new AssetManager(mockProvider.Object);

    // Act
    assetManager.Load("test.asset");

    // Assert
    mockProvider.Verify(p => p.Load("test.asset"), Times.Once);
}
```

---

## 各包测试要点

| 包 | 测试重点 |
|------|----------|
| `Gnosis.Core` | 数学库精度、集合边界条件、内存分配器正确性 |
| `Gnosis.IR` | 优化 Pass 正确性、IR 验证、字节码发射 |
| `Gnosis.Runtime` | VM 指令执行、Interop 调用、协程调度、热重载 |
| `Gnosis.ECS` | 实体生命周期、Archetype 存储、查询正确性、并行安全 |
| `Gnosis.Asset` | VFS 路径解析、格式解析、增量构建 |
| `Gnosis.Graphic` | RHI 后端一致性、着色器编译、管线状态 |
| `Gnosis.Network` | 传输可靠性、同步一致性、预测和解 |
| `Gnosis.Database` | 事务 ACID、WAL 恢复、B+ 树正确性 |
| `Gnosis.Physics` | 碰撞检测精度、约束求解、查询正确性 |
| `Gnosis.Security` | 加密正确性、完整性校验、混淆不可逆 |

---

## 性能测试

### BenchmarkDotNet

使用 BenchmarkDotNet 进行性能基准测试：

```csharp
[MemoryDiagnoser]
public class ArchetypeBenchmark
{
    private World _world;
    private EntityQuery _query;

    [GlobalSetup]
    public void Setup()
    {
        _world = new World();
        for (int i = 0; i < 10000; i++)
        {
            _world.CreateEntity<Position, Velocity>();
        }
        _query = _world.Query<Position, Velocity>();
    }

    [Benchmark]
    public void IterateArchetype()
    {
        foreach (ref var (pos, vel) in _query)
        {
            pos.X += vel.Vx;
        }
    }
}
```

### 运行基准测试

```bash
dotnet run --project projects/Gnosis.Benchmarks -c Release
```

---

## 测试覆盖率目标

| 包 | 目标覆盖率 |
|------|-----------|
| `Gnosis.Core` | ≥ 90% |
| `Gnosis.IR` | ≥ 85% |
| `Gnosis.Runtime` | ≥ 85% |
| `Gnosis.ECS` | ≥ 90% |
| `Gnosis.Database` | ≥ 85% |
| 其他包 | ≥ 80% |
