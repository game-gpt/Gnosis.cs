namespace Gnosis.Toolchain.Compiler;

/// <summary>
///     Gnosis 游戏帧预算成本模型，用于指导 Nyar 编译器的优化决策
/// </summary>
public static class GnosisCostHook
{
    /// <summary>
    ///     目标帧时间（毫秒），默认 16.67ms 对应 60 FPS
    /// </summary>
    public const double TargetFrameTimeMs = 1000.0 / 60.0;

    /// <summary>
    ///     每帧最大允许指令数
    /// </summary>
    public const long MaxInstructionsPerFrame = 100000;

    /// <summary>
    ///     ECS 操作成本权重
    /// </summary>
    public static class EcsCosts
    {
        public const double EntityCreate = 10.0;
        public const double EntityDestroy = 5.0;
        public const double ComponentAdd = 8.0;
        public const double ComponentGet = 2.0;
        public const double ComponentSet = 3.0;
        public const double ComponentRemove = 6.0;
        public const double QueryExecute = 15.0;
        public const double QueryIterate = 1.0;
    }

    /// <summary>
    ///     AI 操作成本权重
    /// </summary>
    public static class AiCosts
    {
        public const double BehaviorTreeTick = 20.0;
        public const double Pathfinding = 50.0;
        public const double DecisionMaking = 30.0;
    }

    /// <summary>
    ///     渲染操作成本权重
    /// </summary>
    public static class RenderingCosts
    {
        public const double DrawCall = 100.0;
        public const double ShaderSwitch = 80.0;
        public const double TextureBind = 40.0;
    }

    /// <summary>
    ///     物理操作成本权重
    /// </summary>
    public static class PhysicsCosts
    {
        public const double Raycast = 25.0;
        public const double CollisionCheck = 15.0;
        public const double RigidBodyUpdate = 10.0;
    }

    /// <summary>
    ///     网络操作成本权重
    /// </summary>
    public static class NetworkCosts
    {
        public const double RpcCall = 5.0;
        public const double StateSync = 8.0;
        public const double Broadcast = 20.0;
    }

    /// <summary>
    ///     内存操作成本权重
    /// </summary>
    public static class MemoryCosts
    {
        public const double Allocation = 3.0;
        public const double Deallocation = 2.0;
        public const double Copy = 1.0;
    }

    /// <summary>
    ///     计算给定操作序列的预估总成本
    /// </summary>
    public static double CalculateTotalCost(IEnumerable<GnosisOperation> operations)
    {
        return operations.Sum(op => op.Cost);
    }

    /// <summary>
    ///     检查操作序列是否能在帧预算内完成
    /// </summary>
    public static bool IsWithinFrameBudget(IEnumerable<GnosisOperation> operations)
    {
        return CalculateTotalCost(operations) <= MaxInstructionsPerFrame;
    }
}

/// <summary>
///     Gnosis 操作描述
/// </summary>
public sealed class GnosisOperation
{
    public string Category { get; }
    public string Name { get; }
    public double Cost { get; }

    public GnosisOperation(string category, string name, double cost)
    {
        Category = category;
        Name = name;
        Cost = cost;
    }
}
