namespace Gnosis.Runtime.Reflection;

/// <summary>
/// 动态调用器，支持运行时动态调用 gg 函数和原生函数
/// </summary>
public sealed class DynamicInvoker
{
    #region Fields

    private readonly VM.VMState _vmState;
    private readonly VM.NativeFunctionRegistry _nativeRegistry;

    #endregion

    #region Constructors

    public DynamicInvoker(VM.VMState vmState, VM.NativeFunctionRegistry nativeRegistry)
    {
        _vmState = vmState;
        _nativeRegistry = nativeRegistry;
    }

    #endregion

    #region 动态调用 gg 函数

    /// <summary>
    /// 按名称动态调用模块内的 gg 函数
    /// </summary>
    public object? InvokeFunction(string moduleName, string functionName, params object?[] args)
    {
        var module = _vmState.GetModule(moduleName);

        if (module is null)
        {
            throw new VM.VMModuleNotFoundException(moduleName);
        }

        var funcInfo = FindFunction(module, functionName);

        if (funcInfo is null)
        {
            throw new VM.VMRuntimeException($"函数未找到：{moduleName}::{functionName}");
        }

        return InvokeAtOffset(module, funcInfo.EntryOffset, funcInfo.ParameterCount, args);
    }

    /// <summary>
    /// 按偏移量动态调用函数
    /// </summary>
    public object? InvokeAtOffset(VM.IModule module, int entryOffset, int paramCount, params object?[] args)
    {
        _vmState.StackInternal.PushFrame(_vmState.IP, _vmState.StackInternal.SP, Math.Max(paramCount, args.Length));

        for (var i = 0; i < args.Length; i++)
        {
            _vmState.Push(args[i]);
        }

        _vmState.IP = entryOffset;

        return null;
    }

    #endregion

    #region 动态调用原生函数

    /// <summary>
    /// 按 ID 动态调用原生函数
    /// </summary>
    public object? InvokeNative(int functionId, params object?[] args)
    {
        var func = _nativeRegistry.Get(functionId);

        if (func is null)
        {
            throw new VM.VMRuntimeException($"原生函数未找到，ID: {functionId}");
        }

        var actualArgs = new object?[func.ParameterCount];

        for (var i = 0; i < Math.Min(args.Length, actualArgs.Length); i++)
        {
            actualArgs[i] = args[i];
        }

        func.Execute(_vmState, actualArgs);
        return _vmState.Pop();
    }

    /// <summary>
    /// 按名称动态调用原生函数
    /// </summary>
    public object? InvokeNative(string functionName, params object?[] args)
    {
        var func = _nativeRegistry.Get(functionName);

        if (func is null)
        {
            throw new VM.VMRuntimeException($"原生函数未找到：{functionName}");
        }

        var actualArgs = new object?[func.ParameterCount];

        for (var i = 0; i < Math.Min(args.Length, actualArgs.Length); i++)
        {
            actualArgs[i] = args[i];
        }

        func.Execute(_vmState, actualArgs);
        return _vmState.Pop();
    }

    #endregion

    #region 辅助方法

    private static VM.ModuleFunctionInfo? FindFunction(VM.IModule module, string name)
    {
        foreach (var func in module.Functions)
        {
            if (func.Name == name)
            {
                return func;
            }
        }

        return null;
    }

    #endregion
}
