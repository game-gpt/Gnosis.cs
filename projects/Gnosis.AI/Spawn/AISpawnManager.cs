using Gnosis.AI.State;

namespace Gnosis.AI.Spawn;

/// <summary>
/// AI 生成管理器实现，管理 AI 实体的池化与生成
/// </summary>
public sealed class AISpawnManager : IAISpawnManager
{
    #region 字段

    private readonly Dictionary<string, IAIController> _activeEntities = new();
    private readonly Dictionary<string, Queue<IAIController>> _poolsByType = new();
    private readonly List<(string EntityId, float RemainingTime)> _pendingSpawns = new();
    private int _nextEntityId;

    #endregion

    #region 属性

    /// <summary>
    /// 当前存活 AI 数量
    /// </summary>
    public int ActiveCount => _activeEntities.Count;

    /// <summary>
    /// 池中可用 AI 数量
    /// </summary>
    public int PooledCount
    {
        get
        {
            int count = 0;

            foreach (var pool in _poolsByType)
            {
                count += pool.Value.Count;
            }

            return count;
        }
    }

    /// <summary>
    /// 最大 AI 数量
    /// </summary>
    public int MaxCount { get; }

    /// <summary>
    /// AI 控制器工厂
    /// </summary>
    public Func<string, IAIController> ControllerFactory { get; set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建 AI 生成管理器
    /// </summary>
    /// <param name="maxCount">最大 AI 数量</param>
    /// <param name="controllerFactory">AI 控制器工厂</param>
    public AISpawnManager(int maxCount = 100, Func<string, IAIController>? controllerFactory = null)
    {
        MaxCount = maxCount;
        ControllerFactory = controllerFactory ?? (typeName => new AIController());
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 生成 AI 实体
    /// </summary>
    /// <param name="parameters">生成参数</param>
    /// <returns>实体 ID</returns>
    public string Spawn(AISpawnParams parameters)
    {
        if (_activeEntities.Count >= MaxCount)
        {
            return string.Empty;
        }

        if (parameters.SpawnDelay > 0)
        {
            string pendingId = GenerateEntityId();
            _pendingSpawns.Add((pendingId, parameters.SpawnDelay));
            return pendingId;
        }

        return SpawnImmediate(parameters);
    }

    /// <summary>
    /// 回收 AI 实体到池中
    /// </summary>
    /// <param name="entityId">实体 ID</param>
    public void Despawn(string entityId)
    {
        if (!_activeEntities.TryGetValue(entityId, out var controller))
        {
            return;
        }

        controller.Reset();
        _activeEntities.Remove(entityId);

        string typeName = GetTypeNameForController(controller);

        if (!_poolsByType.TryGetValue(typeName, out var pool))
        {
            pool = new Queue<IAIController>();
            _poolsByType[typeName] = pool;
        }

        pool.Enqueue(controller);
    }

    /// <summary>
    /// 预热对象池
    /// </summary>
    /// <param name="parameters">生成参数</param>
    /// <param name="count">预热数量</param>
    public void Preload(AISpawnParams parameters, int count)
    {
        if (!_poolsByType.TryGetValue(parameters.TypeName, out var pool))
        {
            pool = new Queue<IAIController>();
            _poolsByType[parameters.TypeName] = pool;
        }

        for (int i = 0; i < count; i++)
        {
            var controller = ControllerFactory(parameters.TypeName);
            pool.Enqueue(controller);
        }
    }

    /// <summary>
    /// 清空对象池
    /// </summary>
    public void ClearPool()
    {
        _poolsByType.Clear();
    }

    /// <summary>
    /// 更新生成管理器（处理延迟生成）
    /// </summary>
    /// <param name="delta">帧间隔时间</param>
    public void Update(float delta)
    {
        for (int i = _pendingSpawns.Count - 1; i >= 0; i--)
        {
            var (entityId, remaining) = _pendingSpawns[i];
            remaining -= delta;

            if (remaining <= 0)
            {
                _pendingSpawns.RemoveAt(i);
            }
            else
            {
                _pendingSpawns[i] = (entityId, remaining);
            }
        }
    }

    /// <summary>
    /// 获取活跃的 AI 控制器
    /// </summary>
    /// <param name="entityId">实体 ID</param>
    /// <returns>AI 控制器，不存在则返回 null</returns>
    public IAIController? GetActiveController(string entityId)
    {
        if (_activeEntities.TryGetValue(entityId, out var controller))
        {
            return controller;
        }

        return null;
    }

    /// <summary>
    /// 获取所有活跃的 AI 控制器
    /// </summary>
    /// <returns>AI 控制器列表</returns>
    public IReadOnlyList<IAIController> GetActiveControllers()
    {
        return _activeEntities.Values.ToList().AsReadOnly();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 立即生成 AI 实体
    /// </summary>
    private string SpawnImmediate(AISpawnParams parameters)
    {
        IAIController controller;

        if (parameters.UsePool && _poolsByType.TryGetValue(parameters.TypeName, out var pool) && pool.Count > 0)
        {
            controller = pool.Dequeue();
        }
        else
        {
            controller = ControllerFactory(parameters.TypeName);
        }

        string entityId = GenerateEntityId();
        _activeEntities[entityId] = controller;

        return entityId;
    }

    /// <summary>
    /// 生成唯一实体 ID
    /// </summary>
    private string GenerateEntityId()
    {
        _nextEntityId++;
        return $"ai_{_nextEntityId}";
    }

    /// <summary>
    /// 获取控制器对应的类型名称
    /// </summary>
    private static string GetTypeNameForController(IAIController controller)
    {
        return controller.GetType().Name;
    }

    #endregion
}
