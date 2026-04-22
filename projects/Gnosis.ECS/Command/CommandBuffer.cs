using Gnosis.ECS.Entity;

namespace Gnosis.ECS.Command;

/// <summary>
/// 命令缓冲执行结果，包含创建的新实体 ID
/// </summary>
public sealed class CommandBufferResult
{
    /// <summary>
    /// 新创建的实体 ID 列表
    /// </summary>
    public List<EntityId> CreatedEntities { get; } = new();
}

/// <summary>
/// 延迟命令缓冲，允许在系统执行期间安全地修改实体和组件，
/// 命令在 Playback 时按顺序执行。
/// 支持批量创建实体并立即在新实体上添加组件。
/// </summary>
public sealed class CommandBuffer
{
    #region 字段

    private readonly List<ICommand> _commands = [];
    private readonly List<EntityCreationContext> _pendingCreations = [];

    #endregion

    #region 属性

    /// <summary>
    /// 待执行的命令数量
    /// </summary>
    public int PendingCommandCount => _commands.Count;

    #endregion

    #region 公开方法 - 实体操作

    /// <summary>
    /// 创建新实体，返回一个可用于链式添加组件的上下文。
    /// 注意：实体 ID 在 Playback 时才真正分配。
    /// </summary>
    public EntityCreationContext CreateEntity()
    {
        var context = new EntityCreationContext(this);
        _pendingCreations.Add(context);

        return context;
    }

    /// <summary>
    /// 销毁指定实体
    /// </summary>
    public void DestroyEntity(EntityId entityId)
    {
        _commands.Add(new DestroyEntityCommand(entityId));
    }

    #endregion

    #region 公开方法 - 组件操作

    /// <summary>
    /// 为已存在的实体添加组件
    /// </summary>
    public void AddComponent<T>(EntityId entityId, T component) where T : struct
    {
        _commands.Add(new AddComponentCommand<T>(entityId, component));
    }

    /// <summary>
    /// 为已存在的实体移除组件
    /// </summary>
    public void RemoveComponent<T>(EntityId entityId) where T : struct
    {
        _commands.Add(new RemoveComponentCommand<T>(entityId));
    }

    /// <summary>
    /// 设置已存在实体的组件值
    /// </summary>
    public void SetComponent<T>(EntityId entityId, T component) where T : struct
    {
        _commands.Add(new SetComponentCommand<T>(entityId, component));
    }

    #endregion

    #region 公开方法 - 执行与清理

    /// <summary>
    /// 执行所有缓冲的命令，返回执行结果（包含新创建的实体 ID）。
    /// 执行顺序：先处理所有待创建实体及其初始组件，再执行其他命令。
    /// </summary>
    public CommandBufferResult Playback(World.World world)
    {
        var result = new CommandBufferResult();

        // 第一步：创建所有待创建的实体并添加初始组件
        foreach (var creation in _pendingCreations)
        {
            var entityId = world.CreateEntity();
            result.CreatedEntities.Add(entityId);

            foreach (var componentAction in creation.ComponentActions)
            {
                componentAction.Execute(world, entityId);
            }
        }

        _pendingCreations.Clear();

        // 第二步：执行其他命令
        foreach (var command in _commands)
        {
            command.Execute(world);
        }

        _commands.Clear();

        return result;
    }

    /// <summary>
    /// 清空所有待执行的命令
    /// </summary>
    public void Clear()
    {
        _commands.Clear();
        _pendingCreations.Clear();
    }

    #endregion

    #region 内部接口

    private interface ICommand
    {
        void Execute(World.World world);
    }

    internal interface IEntityComponentCommand
    {
        void Execute(World.World world, EntityId entityId);
    }

    #endregion

    #region 内部命令

    private sealed class DestroyEntityCommand(EntityId entityId) : ICommand
    {
        public void Execute(World.World world)
        {
            world.DestroyEntity(entityId);
        }
    }

    private sealed class AddComponentCommand<T>(EntityId entityId, T component) : ICommand where T : struct
    {
        public void Execute(World.World world)
        {
            world.AddComponent(entityId, component);
        }
    }

    private sealed class SetComponentCommand<T>(EntityId entityId, T component) : ICommand where T : struct
    {
        public void Execute(World.World world)
        {
            world.SetComponent(entityId, component);
        }
    }

    private sealed class RemoveComponentCommand<T>(EntityId entityId) : ICommand where T : struct
    {
        public void Execute(World.World world)
        {
            world.RemoveComponent<T>(entityId);
        }
    }

    #endregion

    #region 实体创建上下文

    /// <summary>
    /// 实体创建上下文，支持链式添加初始组件。
    /// 在 CommandBuffer.Playback 时真正创建实体并应用组件。
    /// </summary>
    public sealed class EntityCreationContext
    {
        private readonly CommandBuffer _buffer;
        internal readonly List<IEntityComponentCommand> ComponentActions = [];

        internal EntityCreationContext(CommandBuffer buffer)
        {
            _buffer = buffer;
        }

        /// <summary>
        /// 为新实体添加初始组件（链式调用）
        /// </summary>
        public EntityCreationContext With<T>(T component) where T : struct
        {
            ComponentActions.Add(new AddComponentAction<T>(component));
            return this;
        }
    }

    private sealed class AddComponentAction<T>(T component) : IEntityComponentCommand where T : struct
    {
        public void Execute(World.World world, EntityId entityId)
        {
            world.AddComponent(entityId, component);
        }
    }

    #endregion
}
