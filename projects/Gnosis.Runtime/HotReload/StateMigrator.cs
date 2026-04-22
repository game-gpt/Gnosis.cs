using Gnosis.Runtime.VM;

namespace Gnosis.Runtime.HotReload;

/// <summary>
/// 状态迁移器，负责热重载时的状态迁移
/// </summary>
public class StateMigrator
{
    #region 公开方法

    /// <summary>
    /// 将旧状态迁移到新模块上下文
    /// </summary>
    public void Migrate(IVMState vm, VMStateSnapshot oldSnapshot, ModuleDiff diff)
    {
        MigrateGlobalVariables(vm, oldSnapshot, diff);
    }

    #endregion

    #region 全局变量迁移

    private static void MigrateGlobalVariables(IVMState vm, VMStateSnapshot oldSnapshot, ModuleDiff diff)
    {
        if (vm is not VMState concreteState)
        {
            return;
        }

        foreach (var addedGlobal in diff.AddedGlobals)
        {
            concreteState.SetGlobal(addedGlobal, null);
        }

        foreach (var removedGlobal in diff.RemovedGlobals)
        {
            concreteState.SetGlobal(removedGlobal, null);
        }
    }

    #endregion
}
