using Gnosis.Runtime.VM;

namespace Gnosis.Runtime.Interop;

/// <summary>
/// 引用类型封送器，处理 C# 对象引用与 GGValue 之间的双向转换
/// </summary>
public static class ReferenceMarshaller
{
    #region NativeObject 包装

    /// <summary>
    /// 将 C# 对象包装为 GGValue 可识别的 NativeObject
    /// </summary>
    public static GGValue ToNativeObject(object obj)
    {
        return GGValue.FromNativeObject(obj);
    }

    /// <summary>
    /// 从 GGValue 中提取 C# 原生对象
    /// </summary>
    public static object? FromNativeObject(GGValue value)
    {
        return value.Type == GGValueType.NativeObject ? value.NativeObjectValue : null;
    }

    /// <summary>
    /// 从 GGValue 中提取指定类型的 C# 原生对象
    /// </summary>
    public static T? FromNativeObject<T>(GGValue value) where T : class
    {
        return value.Type == GGValueType.NativeObject ? value.NativeObjectValue as T : null;
    }

    #endregion

    #region 对象引用表

    private static int _nextHandle;
    private static readonly Dictionary<int, object> _handleToObject = new();
    private static readonly Dictionary<object, int> _objectToHandle = new(ReferenceEqualityComparer.Instance);

    /// <summary>
    /// 将 C# 对象注册到引用表，返回句柄
    /// </summary>
    public static int PinObject(object obj)
    {
        if (_objectToHandle.TryGetValue(obj, out var existingHandle))
        {
            return existingHandle;
        }

        var handle = _nextHandle++;
        _handleToObject[handle] = obj;
        _objectToHandle[obj] = handle;
        return handle;
    }

    /// <summary>
    /// 通过句柄获取 C# 对象
    /// </summary>
    public static object? GetPinnedObject(int handle)
    {
        return _handleToObject.GetValueOrDefault(handle);
    }

    /// <summary>
    /// 通过句柄获取指定类型的 C# 对象
    /// </summary>
    public static T? GetPinnedObject<T>(int handle) where T : class
    {
        return _handleToObject.TryGetValue(handle, out var obj) ? obj as T : null;
    }

    /// <summary>
    /// 从引用表中移除对象
    /// </summary>
    public static void UnpinObject(int handle)
    {
        if (_handleToObject.TryGetValue(handle, out var obj))
        {
            _handleToObject.Remove(handle);
            _objectToHandle.Remove(obj);
        }
    }

    /// <summary>
    /// 清空引用表
    /// </summary>
    public static void ClearPinnedObjects()
    {
        _handleToObject.Clear();
        _objectToHandle.Clear();
        _nextHandle = 0;
    }

    #endregion

    #region 综合封送

    /// <summary>
    /// 将任意 C# 值封送为 GGValue
    /// </summary>
    public static GGValue MarshalToGG(object? value)
    {
        return value switch
        {
            null => GGValue.Null,
            bool b => GGValue.FromBool(b),
            sbyte sb => GGValue.FromInt(sb),
            byte ub => GGValue.FromInt(ub),
            short s => GGValue.FromInt(s),
            ushort us => GGValue.FromInt(us),
            int i => GGValue.FromInt(i),
            uint ui => GGValue.FromInt(ui),
            long l => GGValue.FromInt(l),
            float f => GGValue.FromFloat(f),
            double d => GGValue.FromFloat(d),
            string s => GGValue.FromString(new GGString(s)),
            GGString gs => GGValue.FromString(gs),
            GGObject go => GGValue.FromObject(go),
            GGArray ga => GGValue.FromArray(ga),
            GGStruct gst => GGValue.FromStruct(gst),
            GGClosure gc => GGValue.FromClosure(gc),
            _ => GGValue.FromNativeObject(value)
        };
    }

    /// <summary>
    /// 将 GGValue 解封为 C# 对象
    /// </summary>
    public static object? MarshalFromGG(GGValue value)
    {
        return value.Type switch
        {
            GGValueType.Null => null,
            GGValueType.Int => value.IntValue,
            GGValueType.Float => value.FloatValue,
            GGValueType.Bool => value.BoolValue,
            GGValueType.String => value.StringValue?.Value,
            GGValueType.Object => value.ObjectValue,
            GGValueType.Array => value.ArrayValue,
            GGValueType.Struct => value.StructValue,
            GGValueType.Closure => value.ClosureValue,
            GGValueType.NativeObject => value.NativeObjectValue,
            _ => null
        };
    }

    #endregion
}

/// <summary>
/// 引用相等比较器
/// </summary>
internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
{
    public static readonly ReferenceEqualityComparer Instance = new();

    public new bool Equals(object? x, object? y)
    {
        return ReferenceEquals(x, y);
    }

    public int GetHashCode(object obj)
    {
        return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
