using Gnosis.ECS.Entity;

namespace Gnosis.ECS.Command;

/// <summary>
/// 延迟命令缓冲，允许在系统执行期间安全地修改实体和组件，
/// 命令在 Playback 时按顺序执行
/// </summary>
public sealed class CommandBuffer
{
    #region 字段

    private readonly List<ICommand> _commands = [];

    #endregion

    #region 属性

    public int PendingCommandCount => _commands.Count;

    #endregion

    #region 公开方法

    public void CreateEntity(EntityId entityId)
    {
        _commands.Add(new CreateEntityCommand(entityId));
    }

    public void DestroyEntity(EntityId entityId)
    {
        _commands.Add(new DestroyEntityCommand(entityId));
    }

    public void AddComponent<T>(EntityId entityId, T component) where T : struct
    {
        _commands.Add(new AddComponentCommand<T>(entityId, component));
    }

    public void RemoveComponent<T>(EntityId entityId) where T : struct
    {
        _commands.Add(new RemoveComponentCommand<T>(entityId));
    }

    public void Playback(World.World world)
    {
        foreach (var command in _commands)
        {
            command.Execute(world);
        }

        _commands.Clear();
    }

    public void Clear() => _commands.Clear();

    #endregion

    #region 内部接口

    private interface ICommand
    {
        void Execute(World.World world);
    }

    #endregion

    #region 内部命令

    private sealed class CreateEntityCommand : ICommand
    {
        private readonly EntityId _entityId;

        public CreateEntityCommand(EntityId entityId)
        {
            _entityId = entityId;
        }

        public void Execute(World.World world)
        {
        }
    }

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

    private sealed class RemoveComponentCommand<T>(EntityId entityId) : ICommand where T : struct
    {
        public void Execute(World.World world)
        {
            world.RemoveComponent<T>(entityId);
        }
    }

    #endregion
}
