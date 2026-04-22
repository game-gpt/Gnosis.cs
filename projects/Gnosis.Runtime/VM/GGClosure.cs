namespace Gnosis.Runtime.VM;

/// <summary>
/// 闭包，捕获 Upvalues，支持 GC 追踪
/// </summary>
public class GGClosure : IGCObject
{
    #region 属性

    /// <summary>
    /// 函数地址
    /// </summary>
    public int FunctionAddress { get; }

    /// <summary>
    /// Upvalues 数组
    /// </summary>
    public object?[] Upvalues { get; }

    #endregion

    #region IGCObject 实现

    /// <summary>
    /// 对象 ID
    /// </summary>
    public int ObjectId { get; set; }

    /// <summary>
    /// GC 标记
    /// </summary>
    public bool IsMarked { get; set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用函数地址和 Upvalue 数量初始化闭包
    /// </summary>
    /// <param name="functionAddress">函数地址</param>
    /// <param name="upvalueCount">Upvalue 数量</param>
    public GGClosure(int functionAddress, int upvalueCount)
    {
        FunctionAddress = functionAddress;
        Upvalues = new object?[upvalueCount];
        IsMarked = false;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 获取指定索引的 Upvalue
    /// </summary>
    /// <param name="index">Upvalue 索引</param>
    /// <returns>Upvalue 值</returns>
    public object? GetUpvalue(int index)
    {
        if (index < 0 || index >= Upvalues.Length)
        {
            throw new VMIndexOutOfBoundsException(index, Upvalues.Length);
        }

        return Upvalues[index];
    }

    /// <summary>
    /// 设置指定索引的 Upvalue
    /// </summary>
    /// <param name="index">Upvalue 索引</param>
    /// <param name="value">Upvalue 值</param>
    public void SetUpvalue(int index, object? value)
    {
        if (index < 0 || index >= Upvalues.Length)
        {
            throw new VMIndexOutOfBoundsException(index, Upvalues.Length);
        }

        Upvalues[index] = value;
    }

    /// <summary>
    /// 获取 GC 引用
    /// </summary>
    public IEnumerable<IGCObject?> GetGCReferences()
    {
        foreach (var v in Upvalues)
        {
            if (v is IGCObject gcObj)
            {
                yield return gcObj;
            }
        }
    }

    /// <summary>
    /// 获取所有引用（包括非 GC 对象）
    /// </summary>
    public IEnumerable<object?> GetReferences()
    {
        return Upvalues.Where(v => v is GGObject or GGArray or GGString or GGStruct or GGClosure);
    }

    #endregion
}
