using Gnosis.Runtime.VM;

namespace Gnosis.Runtime.HotReload;

/// <summary>
/// 模块热重载器，负责运行时模块替换和状态迁移
/// </summary>
public class ModuleReloader : IModuleReloader
{
    #region Fields

    private readonly Dictionary<string, IModule> _loadedModules = new();
    private readonly StateMigrator _migrator;
    private readonly MetadataComparer _comparer;

    private bool _isReloading;
    private readonly HashSet<string> _suspendedModules = new();

    #endregion

    #region Properties

    public bool IsReloading => _isReloading;

    public event EventHandler<ModuleReloadEventArgs>? ModuleReloading;
    public event EventHandler<ModuleReloadEventArgs>? ModuleReloaded;

    #endregion

    #region Constructors

    public ModuleReloader()
    {
        _migrator = new StateMigrator();
        _comparer = new MetadataComparer();
        _isReloading = false;
    }

    #endregion

    #region IModuleReloader 方法

    /// <summary>
    /// 按名称重载模块
    /// </summary>
    public bool ReloadModule(IVMState vm, string moduleName)
    {
        var oldModule = vm.GetModule(moduleName);

        if (oldModule is null)
        {
            return false;
        }

        return ReloadModule(vm, oldModule, moduleName);
    }

    /// <summary>
    /// 用新模块替换旧模块
    /// </summary>
    public bool ReloadModule(IVMState vm, IModule newModule)
    {
        var moduleName = newModule.Name;
        var oldModule = vm.GetModule(moduleName);

        if (oldModule is null)
        {
            return false;
        }

        return ReloadModule(vm, oldModule, newModule);
    }

    /// <summary>
    /// 判断指定模块是否可以安全重载
    /// </summary>
    public bool CanReload(string moduleName)
    {
        return !_suspendedModules.Contains(moduleName);
    }

    /// <summary>
    /// 挂起指定模块的执行
    /// </summary>
    public void SuspendModule(string moduleName)
    {
        _suspendedModules.Add(moduleName);
    }

    /// <summary>
    /// 恢复指定模块的执行
    /// </summary>
    public void ResumeModule(string moduleName)
    {
        _suspendedModules.Remove(moduleName);
    }

    #endregion

    #region 内部方法

    private bool ReloadModule(IVMState vm, IModule oldModule, string moduleName)
    {
        if (_isReloading)
        {
            return false;
        }

        if (_suspendedModules.Contains(moduleName))
        {
            return false;
        }

        _isReloading = true;

        try
        {
            var args = new ModuleReloadEventArgs(moduleName, oldModule, null, true);
            ModuleReloading?.Invoke(this, args);

            if (!args.Success)
            {
                OnReloadFailed(moduleName, oldModule, args.ErrorMessage);
                return false;
            }

            var snapshot = vm.CreateSnapshot();

            vm.UnloadModule(moduleName);

            if (vm.CurrentModule == oldModule || vm.CurrentModule?.Name == moduleName)
            {
                vm.Reset();
            }

            OnReloadCompleted(moduleName, oldModule, null, snapshot);
            return true;
        }
        catch (Exception ex)
        {
            OnReloadFailed(moduleName, oldModule, ex.Message);
            return false;
        }
        finally
        {
            _isReloading = false;
        }
    }

    private bool ReloadModule(IVMState vm, IModule oldModule, IModule newModule)
    {
        if (_isReloading)
        {
            return false;
        }

        var moduleName = newModule.Name;

        if (_suspendedModules.Contains(moduleName))
        {
            return false;
        }

        _isReloading = true;

        try
        {
            var diff = _comparer.Compare(oldModule, newModule);

            if (!diff.IsCompatible)
            {
                OnReloadFailed(moduleName, oldModule, $"模块不兼容：{diff.IncompatibilityReason}");
                return false;
            }

            var args = new ModuleReloadEventArgs(moduleName, oldModule, newModule, true);
            ModuleReloading?.Invoke(this, args);

            if (!args.Success)
            {
                OnReloadFailed(moduleName, oldModule, args.ErrorMessage);
                return false;
            }

            var snapshot = vm.CreateSnapshot();

            vm.UnloadModule(oldModule.Name);
            vm.LoadModule(newModule);

            if (vm.CurrentModule == oldModule || vm.CurrentModule?.Name == moduleName)
            {
                if (vm is VMState concreteState)
                {
                    concreteState.SetCurrentModule(newModule);
                }
                _migrator.Migrate(vm, snapshot, diff);
            }

            OnReloadCompleted(moduleName, oldModule, newModule, snapshot);
            return true;
        }
        catch (Exception ex)
        {
            OnReloadFailed(moduleName, oldModule, ex.Message);
            return false;
        }
        finally
        {
            _isReloading = false;
        }
    }

    private void OnReloadCompleted(string moduleName, IModule? oldModule, IModule? newModule, VMStateSnapshot snapshot)
    {
        var args = new ModuleReloadEventArgs(moduleName, oldModule, newModule, true);
        ModuleReloaded?.Invoke(this, args);
    }

    private void OnReloadFailed(string moduleName, IModule? oldModule, string? errorMessage)
    {
        var args = new ModuleReloadEventArgs(moduleName, oldModule, null, false, errorMessage);
        ModuleReloaded?.Invoke(this, args);
    }

    #endregion
}
