namespace Gnosis.Runtime.VM;

/// <summary>
/// 动态数组，支持 GC 追踪
/// </summary>
public class GGArray : IGCObject
{
    private object?[] _elements;
    private int _count;

    #region 属性

    /// <summary>
    /// 元素数量
    /// </summary>
    public int Count => _count;

    /// <summary>
    /// 容量
    /// </summary>
    public int Capacity => _elements.Length;

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

    #region 索引器

    /// <summary>
    /// 按索引访问元素
    /// </summary>
    public object? this[int index]
    {
        get
        {
            if (index < 0 || index >= _count)
            {
                throw new VMIndexOutOfBoundsException(index, _count);
            }

            return _elements[index];
        }
        set
        {
            if (index < 0 || index >= _count)
            {
                throw new VMIndexOutOfBoundsException(index, _count);
            }

            _elements[index] = value;
        }
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用初始容量初始化数组
    /// </summary>
    /// <param name="capacity">初始容量</param>
    public GGArray(int capacity)
    {
        _elements = new object?[capacity];
        _count = 0;
        IsMarked = false;
    }

    #endregion

    #region 公有方法

    /// <summary>
    /// 添加元素到数组末尾
    /// </summary>
    /// <param name="value">要添加的元素</param>
    public void Add(object? value)
    {
        if (_count >= _elements.Length)
        {
            Array.Resize(ref _elements, _elements.Length * 2);
        }

        _elements[_count++] = value;
    }

    /// <summary>
    /// 获取 GC 引用
    /// </summary>
    public IEnumerable<IGCObject?> GetGCReferences()
    {
        for (var i = 0; i < _count; i++)
        {
            if (_elements[i] is IGCObject gcObj)
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
        for (var i = 0; i < _count; i++)
        {
            if (_elements[i] is GGObject or GGArray or GGString or GGStruct or GGClosure)
            {
                yield return _elements[i];
            }
        }
    }

    #endregion
}
