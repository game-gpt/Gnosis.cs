using Gnosis.Runtime.VM;

namespace Gnosis.Runtime.Interop;

/// <summary>
/// 引用类型封送器，处理 C# 对象与 GGValue 之间的双向转换
/// </summary>
public static class ReferenceMarshaller
{
    private static readonly Dictionary<int, object> _handleToObject = new();
    private static readonly Dictionary<object, int> _objectToHandle = new();
    private static int _nextHandle;

    /// <summary>
    /// 将 C# 对象封送为 GGValue
    /// </summary>
    public static GGValue MarshalToGG(object? value)
    {
        return value switch
        {
            null => GGValue.Null,
            int i => GGValue.FromInt(i),
            long l => GGValue.FromInt(l),
            float f => GGValue.FromFloat(f),
            double d => GGValue.FromFloat(d),
            bool b => GGValue.FromBool(b),
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
    /// 将 GGValue 解封送为 C# 对象
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
            GGValueType.Entity => value.EntityId,
            GGValueType.NativeObject => value.NativeObjectValue,
            _ => value.Reference
        };
    }

    /// <summary>
    /// Pin 住对象，返回句柄（防止 GC 回收被 gg 引用的 C# 对象）
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
    /// 通过句柄取回对象
    /// </summary>
    public static T? GetPinnedObject<T>(int handle) where T : class
    {
        return _handleToObject.TryGetValue(handle, out var obj) ? obj as T : null;
    }

    /// <summary>
    /// 释放 Pin 住的对象
    /// </summary>
    public static void UnpinObject(int handle)
    {
        if (_handleToObject.TryGetValue(handle, out var obj))
        {
            _objectToHandle.Remove(obj);
            _handleToObject.Remove(handle);
        }
    }

    /// <summary>
    /// 清除所有 Pin 住的对象
    /// </summary>
    public static void ClearPinnedObjects()
    {
        _handleToObject.Clear();
        _objectToHandle.Clear();
        _nextHandle = 0;
    }
}
