using Gnosis.Runtime.Interop;
using Gnosis.Runtime.VM;

namespace Gnosis.Runtime.Reflection;

/// <summary>
/// 动态调用器，支持运行时动态调用 gg 函数和原生函数
/// </summary>
public sealed class DynamicInvoker
{
    #region Fields

    private readonly VMState _vmState;
    private readonly NativeFunctionRegistry _nativeRegistry;

    #endregion

    #region Constructors

    public DynamicInvoker(VMState vmState, NativeFunctionRegistry nativeRegistry)
    {
        _vmState = vmState;
        _nativeRegistry = nativeRegistry;
    }

    #endregion

    #region 动态调用 gg 函数

    /// <summary>
    /// 按名称动态调用模块内的 gg 函数
    /// </summary>
    public GGValue InvokeFunction(string moduleName, string functionName, params GGValue[] args)
    {
        var module = _vmState.GetModule(moduleName);

        if (module is null)
        {
            throw new VMModuleNotFoundException(moduleName);
        }

        var funcInfo = FindFunction(module, functionName);

        if (funcInfo is null)
        {
            throw new VMRuntimeException($"函数未找到：{moduleName}::{functionName}");
        }

        return InvokeAtOffset(module, funcInfo.EntryOffset, funcInfo.ParameterCount, args);
    }

    /// <summary>
    /// 按偏移量动态调用函数
    /// </summary>
    public GGValue InvokeAtOffset(IModule module, int entryOffset, int paramCount, params GGValue[] args)
    {
        _vmState.StackInternal.PushFrame(_vmState.IP, _vmState.StackInternal.SP, Math.Max(paramCount, args.Length));

        for (var i = 0; i < args.Length; i++)
        {
            _vmState.Push(args[i]);
        }

        _vmState.IP = entryOffset;

        return GGValue.Null;
    }

    #endregion

    #region 动态调用原生函数

    /// <summary>
    /// 按 ID 动态调用原生函数
    /// </summary>
    public GGValue InvokeNative(int functionId, params GGValue[] args)
    {
        var func = _nativeRegistry.Get(functionId);

        if (func is null)
        {
            throw new VMRuntimeException($"原生函数未找到，ID: {functionId}");
        }

        var actualArgs = new GGValue[func.ParameterCount];

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
    public GGValue InvokeNative(string functionName, params GGValue[] args)
    {
        var func = _nativeRegistry.Get(functionName);

        if (func is null)
        {
            throw new VMRuntimeException($"原生函数未找到：{functionName}");
        }

        var actualArgs = new GGValue[func.ParameterCount];

        for (var i = 0; i < Math.Min(args.Length, actualArgs.Length); i++)
        {
            actualArgs[i] = args[i];
        }

        func.Execute(_vmState, actualArgs);
        return _vmState.Pop();
    }

    #endregion

    #region C# 互操作便捷方法

    /// <summary>
    /// 使用 C# 对象参数调用原生函数（自动封送）
    /// </summary>
    public object? InvokeNativeBoxed(int functionId, params object?[] args)
    {
        var ggArgs = Marshaller.ToGGValues(args);
        var result = InvokeNative(functionId, ggArgs);
        return ReferenceMarshaller.MarshalFromGG(result);
    }

    /// <summary>
    /// 使用 C# 对象参数按名称调用原生函数（自动封送）
    /// </summary>
    public object? InvokeNativeBoxed(string functionName, params object?[] args)
    {
        var ggArgs = Marshaller.ToGGValues(args);
        var result = InvokeNative(functionName, ggArgs);
        return ReferenceMarshaller.MarshalFromGG(result);
    }

    #endregion

    #region 辅助方法

    private static ModuleFunctionInfo? FindFunction(IModule module, string name)
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
