namespace Gnosis.Runtime.VM;

public interface IModuleReloader
{
    bool IsReloading { get; }
    event EventHandler<ModuleReloadEventArgs>? ModuleReloading;
    event EventHandler<ModuleReloadEventArgs>? ModuleReloaded;
    bool ReloadModule(IVMState vm, string moduleName);
    bool ReloadModule(IVMState vm, IModule newModule);
    bool CanReload(string moduleName);
    void SuspendModule(string moduleName);
    void ResumeModule(string moduleName);
}

public class ModuleReloadEventArgs : EventArgs
{
    public string ModuleName { get; }
    public IModule? OldModule { get; }
    public IModule? NewModule { get; }
    public bool Success { get; }
    public string? ErrorMessage { get; }

    public ModuleReloadEventArgs(string moduleName, IModule? oldModule, IModule? newModule, bool success, string? errorMessage = null)
    {
        ModuleName = moduleName;
        OldModule = oldModule;
        NewModule = newModule;
        Success = success;
        ErrorMessage = errorMessage;
    }
}
