using Gnosis.Runtime.VM;

namespace Gnosis.Runtime.HotReload;

/// <summary>
/// 状态迁移器，负责在模块热重载时迁移虚拟机状态
/// </summary>
public sealed class StateMigrator
{
    #region Fields

    private readonly Dictionary<string, MigrationRule> _migrationRules = new();

    #endregion

    #region Properties

    /// <summary>
    /// 迁移规则数量
    /// </summary>
    public int RuleCount => _migrationRules.Count;

    #endregion

    #region 迁移规则管理

    /// <summary>
    /// 添加迁移规则
    /// </summary>
    public void AddMigrationRule(string variableName, MigrationStrategy strategy)
    {
        _migrationRules[variableName] = new MigrationRule(variableName, strategy);
    }

    /// <summary>
    /// 移除迁移规则
    /// </summary>
    public void RemoveMigrationRule(string variableName)
    {
        _migrationRules.Remove(variableName);
    }

    /// <summary>
    /// 清除所有迁移规则
    /// </summary>
    public void ClearMigrationRules()
    {
        _migrationRules.Clear();
    }

    #endregion

    #region 状态迁移

    /// <summary>
    /// 执行状态迁移，将快照中的状态应用到虚拟机
    /// </summary>
    public void Migrate(IVMState vm, VMStateSnapshot snapshot, ModuleDiff diff)
    {
        if (vm is not VMState concreteState)
        {
            return;
        }

        concreteState.IP = snapshot.IP;

        foreach (var frame in snapshot.CallFrames)
        {
            var migratedLocals = MigrateLocals(frame.Locals);
            concreteState.StackInternal.PushFrame(frame.ReturnAddress, frame.BasePointer, migratedLocals.Length);

            var currentFrame = concreteState.StackInternal.CurrentFrame;
            if (currentFrame.HasValue)
            {
                for (var i = 0; i < migratedLocals.Length && i < currentFrame.Value.Locals.Length; i++)
                {
                    currentFrame.Value.Locals[i] = migratedLocals[i];
                }
            }
        }
    }

    /// <summary>
    /// 执行状态迁移，将旧状态中的值按规则迁移到新状态
    /// </summary>
    public VMStateSnapshot MigrateState(VMStateSnapshot oldState, IModule newModule)
    {
        var newStack = new GGValue[oldState.Stack.Length];
        for (var i = 0; i < oldState.Stack.Length; i++)
        {
            newStack[i] = oldState.Stack[i];
        }

        var newFrames = new CallFrameInfo[oldState.CallFrames.Length];

        for (var i = 0; i < oldState.CallFrames.Length; i++)
        {
            var oldFrame = oldState.CallFrames[i];
            var migratedLocals = MigrateLocals(oldFrame.Locals);
            newFrames[i] = new CallFrameInfo(oldFrame.ReturnAddress, oldFrame.BasePointer, migratedLocals);
        }

        return new VMStateSnapshot(
            oldState.IP,
            oldState.SP,
            newStack,
            newFrames,
            newModule.Name,
            oldState.ModuleCount);
    }

    private GGValue[] MigrateLocals(GGValue[] locals)
    {
        var result = new GGValue[locals.Length];

        for (var i = 0; i < locals.Length; i++)
        {
            var varName = $"local_{i}";
            result[i] = _migrationRules.TryGetValue(varName, out var rule) ? ApplyMigration(locals[i], rule) : locals[i];
        }

        return result;
    }

    private static GGValue ApplyMigration(GGValue value, MigrationRule rule)
    {
        return rule.Strategy switch
        {
            MigrationStrategy.Reset => GGValue.Null,
            MigrationStrategy.Preserve => value,
            MigrationStrategy.Convert => value.IsInt ? GGValue.FromFloat(value.IntValue) : value,
            MigrationStrategy.Default => value,
            _ => value
        };
    }

    #endregion
}

/// <summary>
/// 迁移规则
/// </summary>
public sealed class MigrationRule
{
    #region 属性

    /// <summary>
    /// 变量名称
    /// </summary>
    public string VariableName { get; }

    /// <summary>
    /// 迁移策略
    /// </summary>
    public MigrationStrategy Strategy { get; }

    #endregion

    #region 构造函数

    public MigrationRule(string variableName, MigrationStrategy strategy)
    {
        VariableName = variableName;
        Strategy = strategy;
    }

    #endregion
}

/// <summary>
/// 迁移策略枚举
/// </summary>
public enum MigrationStrategy
{
    Reset,
    Preserve,
    Convert,
    Default
}
