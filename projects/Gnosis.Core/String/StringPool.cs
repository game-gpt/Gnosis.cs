using System;
using System.Collections.Generic;

namespace Gnosis.Core.String;

/// <summary>
/// 字符串驻留池，用于共享相同字符串的实例以减少内存分配。
/// 线程安全。
/// </summary>
public class StringPool
{
    private readonly Dictionary<string, string> _pool;
    private readonly object _lock = new();

    /// <summary>
    /// 获取池中驻留的字符串数量。
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _pool.Count;
            }
        }
    }

    /// <summary>
    /// 使用指定的初始容量初始化字符串驻留池。
    /// </summary>
    /// <param name="initialCapacity">内部字典的初始容量，默认为 256。</param>
    public StringPool(int initialCapacity = 256)
    {
        if (initialCapacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCapacity), "初始容量必须大于零");
        }

        _pool = new Dictionary<string, string>(initialCapacity);
    }

    /// <summary>
    /// 将字符串驻留到池中。如果池中已存在相同内容的字符串，则返回池中的实例；
    /// 否则将字符串添加到池中并返回该实例。
    /// </summary>
    /// <param name="value">要驻留的字符串。</param>
    /// <returns>池中的字符串实例。</returns>
    public string Intern(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        lock (_lock)
        {
            if (_pool.TryGetValue(value, out var pooled))
            {
                return pooled;
            }

            _pool[value] = value;
            return value;
        }
    }

    /// <summary>
    /// 将字符范围驻留到池中。如果池中已存在相同内容的字符串，则返回池中的实例；
    /// 否则根据字符范围创建新字符串并添加到池中。
    /// 此方法仅在池中不存在时才分配新字符串。
    /// </summary>
    /// <param name="value">要驻留的字符范围。</param>
    /// <returns>池中的字符串实例。</returns>
    public string Intern(ReadOnlySpan<char> value)
    {
        var tempString = value.ToString();

        lock (_lock)
        {
            if (_pool.TryGetValue(tempString, out var pooled))
            {
                return pooled;
            }

            _pool[tempString] = tempString;
            return tempString;
        }
    }
}
