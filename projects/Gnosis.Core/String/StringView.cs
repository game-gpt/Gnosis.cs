using System;
using System.Collections;
using System.Collections.Generic;

namespace Gnosis.Core.String;

/// <summary>
/// 零拷贝字符串视图，类似于 C++ 的 std::string_view。
/// 不拥有字符串数据，仅持有对原始字符串的引用、偏移量和长度。
/// </summary>
public readonly struct StringView : IEquatable<StringView>
{
    private readonly string? _text;
    private readonly int _offset;
    private readonly int _length;

    /// <summary>
    /// 空的字符串视图。
    /// </summary>
    public static StringView Empty => new(string.Empty);

    /// <summary>
    /// 获取字符串视图的长度。
    /// </summary>
    public int Length => _length;

    /// <summary>
    /// 获取字符串视图是否为空。
    /// </summary>
    public bool IsEmpty => _length == 0;

    /// <summary>
    /// 获取指定索引处的字符。
    /// </summary>
    /// <param name="index">从零开始的索引。</param>
    /// <returns>指定索引处的字符。</returns>
    public char this[int index]
    {
        get
        {
            if ((uint)index >= (uint)_length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"索引 {index} 超出范围 [0, {_length})");
            }

            return _text![_offset + index];
        }
    }

    /// <summary>
    /// 使用完整的字符串初始化字符串视图。
    /// </summary>
    /// <param name="text">源字符串。</param>
    public StringView(string? text)
    {
        _text = text;
        _offset = 0;
        _length = text?.Length ?? 0;
    }

    /// <summary>
    /// 使用字符串的子范围初始化字符串视图。
    /// </summary>
    /// <param name="text">源字符串。</param>
    /// <param name="offset">起始偏移量。</param>
    /// <param name="length">子范围长度。</param>
    public StringView(string? text, int offset, int length)
    {
        if (text is null)
        {
            if (offset != 0 || length != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), "当源字符串为 null 时，偏移量和长度必须为零");
            }

            _text = null;
            _offset = 0;
            _length = 0;
            return;
        }

        if ((uint)offset > (uint)text.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), $"偏移量 {offset} 超出字符串长度 {text.Length}");
        }

        if ((uint)length > (uint)(text.Length - offset))
        {
            throw new ArgumentOutOfRangeException(nameof(length), $"长度 {length} 超出可用范围 {text.Length - offset}");
        }

        _text = text;
        _offset = offset;
        _length = length;
    }

    /// <summary>
    /// 返回从指定偏移量开始、指定长度的子视图（零拷贝）。
    /// </summary>
    /// <param name="offset">起始偏移量。</param>
    /// <param name="length">子视图长度。</param>
    /// <returns>新的字符串视图。</returns>
    public StringView Substring(int offset, int length)
    {
        if ((uint)offset > (uint)_length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), $"偏移量 {offset} 超出视图长度 {_length}");
        }

        if ((uint)length > (uint)(_length - offset))
        {
            throw new ArgumentOutOfRangeException(nameof(length), $"长度 {length} 超出可用范围 {_length - offset}");
        }

        return new StringView(_text, _offset + offset, length);
    }

    /// <summary>
    /// 返回从指定偏移量开始到末尾的子视图（零拷贝）。
    /// </summary>
    /// <param name="offset">起始偏移量。</param>
    /// <returns>新的字符串视图。</returns>
    public StringView Substring(int offset)
    {
        if ((uint)offset > (uint)_length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), $"偏移量 {offset} 超出视图长度 {_length}");
        }

        return new StringView(_text, _offset + offset, _length - offset);
    }

    /// <summary>
    /// 将字符串视图转换为新的字符串实例（分配新内存）。
    /// </summary>
    /// <returns>新分配的字符串。</returns>
    public override string ToString()
    {
        if (_text is null || _length == 0)
        {
            return string.Empty;
        }

        return _text.Substring(_offset, _length);
    }

    /// <summary>
    /// 查找指定字符在字符串视图中首次出现的位置。
    /// </summary>
    /// <param name="value">要查找的字符。</param>
    /// <returns>字符的索引，未找到返回 -1。</returns>
    public int IndexOf(char value)
    {
        if (_text is null || _length == 0)
        {
            return -1;
        }

        var idx = _text.IndexOf(value, _offset, _length);
        return idx >= 0 ? idx - _offset : -1;
    }

    /// <summary>
    /// 从指定起始位置开始，查找指定字符在字符串视图中首次出现的位置。
    /// </summary>
    /// <param name="value">要查找的字符。</param>
    /// <param name="startIndex">搜索起始位置（相对于视图）。</param>
    /// <returns>字符的索引，未找到返回 -1。</returns>
    public int IndexOf(char value, int startIndex)
    {
        if (_text is null || _length == 0)
        {
            return -1;
        }

        if ((uint)startIndex > (uint)_length)
        {
            throw new ArgumentOutOfRangeException(nameof(startIndex), $"起始索引 {startIndex} 超出视图长度 {_length}");
        }

        var remaining = _length - startIndex;
        var idx = _text.IndexOf(value, _offset + startIndex, remaining);
        return idx >= 0 ? idx - _offset : -1;
    }

    /// <summary>
    /// 查找指定子字符串在字符串视图中首次出现的位置。
    /// </summary>
    /// <param name="value">要查找的子字符串。</param>
    /// <returns>子字符串的起始索引，未找到返回 -1。</returns>
    public int IndexOf(string value)
    {
        if (_text is null || _length == 0)
        {
            return -1;
        }

        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        var idx = _text.IndexOf(value, _offset, _length, StringComparison.Ordinal);
        return idx >= 0 ? idx - _offset : -1;
    }

    /// <summary>
    /// 查找指定字符在字符串视图中最后出现的位置。
    /// </summary>
    /// <param name="value">要查找的字符。</param>
    /// <returns>字符的索引，未找到返回 -1。</returns>
    public int LastIndexOf(char value)
    {
        if (_text is null || _length == 0)
        {
            return -1;
        }

        var idx = _text.LastIndexOf(value, _offset + _length - 1, _length);
        return idx >= 0 ? idx - _offset : -1;
    }

    /// <summary>
    /// 判断字符串视图中是否包含指定字符。
    /// </summary>
    /// <param name="value">要查找的字符。</param>
    /// <returns>包含返回 true，否则返回 false。</returns>
    public bool Contains(char value)
    {
        return IndexOf(value) >= 0;
    }

    /// <summary>
    /// 判断字符串视图中是否包含指定子字符串。
    /// </summary>
    /// <param name="value">要查找的子字符串。</param>
    /// <returns>包含返回 true，否则返回 false。</returns>
    public bool Contains(string value)
    {
        return IndexOf(value) >= 0;
    }

    /// <summary>
    /// 判断字符串视图是否以指定的字符串视图开头。
    /// </summary>
    /// <param name="other">要比较的前缀字符串视图。</param>
    /// <returns>以指定前缀开头返回 true，否则返回 false。</returns>
    public bool StartsWith(StringView other)
    {
        if (other._length > _length)
        {
            return false;
        }

        for (var i = 0; i < other._length; i++)
        {
            if (_text![_offset + i] != other._text![other._offset + i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 判断字符串视图是否以指定的字符串开头。
    /// </summary>
    /// <param name="other">要比较的前缀字符串。</param>
    /// <returns>以指定前缀开头返回 true，否则返回 false。</returns>
    public bool StartsWith(string other)
    {
        return StartsWith(new StringView(other));
    }

    /// <summary>
    /// 判断字符串视图是否以指定的字符串视图结尾。
    /// </summary>
    /// <param name="other">要比较的后缀字符串视图。</param>
    /// <returns>以指定后缀结尾返回 true，否则返回 false。</returns>
    public bool EndsWith(StringView other)
    {
        if (other._length > _length)
        {
            return false;
        }

        var start = _length - other._length;
        for (var i = 0; i < other._length; i++)
        {
            if (_text![_offset + start + i] != other._text![other._offset + i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 判断字符串视图是否以指定的字符串结尾。
    /// </summary>
    /// <param name="other">要比较的后缀字符串。</param>
    /// <returns>以指定后缀结尾返回 true，否则返回 false。</returns>
    public bool EndsWith(string other)
    {
        return EndsWith(new StringView(other));
    }

    /// <summary>
    /// 使用指定分隔符拆分字符串视图，返回可枚举的字符串视图集合。
    /// </summary>
    /// <param name="separator">分隔字符。</param>
    /// <returns>字符串视图可枚举集合。</returns>
    public StringViewEnumerable Split(char separator)
    {
        return new StringViewEnumerable(this, separator);
    }

    /// <summary>
    /// 移除字符串视图开头的空白字符。
    /// </summary>
    /// <returns>去除前导空白后的字符串视图。</returns>
    public StringView TrimStart()
    {
        if (_text is null || _length == 0)
        {
            return this;
        }

        var start = _offset;
        var end = _offset + _length;

        while (start < end && char.IsWhiteSpace(_text[start]))
        {
            start++;
        }

        return new StringView(_text, start, end - start);
    }

    /// <summary>
    /// 移除字符串视图末尾的空白字符。
    /// </summary>
    /// <returns>去除尾部空白后的字符串视图。</returns>
    public StringView TrimEnd()
    {
        if (_text is null || _length == 0)
        {
            return this;
        }

        var start = _offset;
        var end = _offset + _length;

        while (end > start && char.IsWhiteSpace(_text[end - 1]))
        {
            end--;
        }

        return new StringView(_text, start, end - start);
    }

    /// <summary>
    /// 移除字符串视图开头和末尾的空白字符。
    /// </summary>
    /// <returns>去除前后空白后的字符串视图。</returns>
    public StringView Trim()
    {
        return TrimStart().TrimEnd();
    }

    /// <summary>
    /// 判断当前字符串视图是否与另一个字符串视图相等。
    /// </summary>
    /// <param name="other">要比较的字符串视图。</param>
    /// <returns>相等返回 true，否则返回 false。</returns>
    public bool Equals(StringView other)
    {
        if (_length != other._length)
        {
            return false;
        }

        if (_text is null && other._text is null)
        {
            return true;
        }

        if (_text is null || other._text is null)
        {
            return false;
        }

        for (var i = 0; i < _length; i++)
        {
            if (_text[_offset + i] != other._text[other._offset + i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 判断当前字符串视图是否与指定的字符串相等。
    /// </summary>
    /// <param name="other">要比较的字符串。</param>
    /// <returns>相等返回 true，否则返回 false。</returns>
    public bool Equals(string? other)
    {
        return Equals(new StringView(other));
    }

    /// <summary>
    /// 判断当前字符串视图是否与指定对象相等。
    /// </summary>
    /// <param name="obj">要比较的对象。</param>
    /// <returns>相等返回 true，否则返回 false。</returns>
    public override bool Equals(object? obj)
    {
        return obj is StringView other && Equals(other);
    }

    /// <summary>
    /// 获取字符串视图的哈希码。
    /// </summary>
    /// <returns>哈希码。</returns>
    public override int GetHashCode()
    {
        if (_text is null || _length == 0)
        {
            return 0;
        }

        var hash = new HashCode();
        for (var i = 0; i < _length; i++)
        {
            hash.Add(_text[_offset + i]);
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// 判断两个字符串视图是否相等。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>相等返回 true，否则返回 false。</returns>
    public static bool operator ==(StringView left, StringView right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// 判断两个字符串视图是否不相等。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>不相等返回 true，否则返回 false。</returns>
    public static bool operator !=(StringView left, StringView right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// 用于迭代 StringView.Split 结果的可枚举结构体。
/// </summary>
public ref struct StringViewEnumerable
{
    private readonly StringView _source;
    private readonly char _separator;

    /// <summary>
    /// 使用源字符串视图和分隔符初始化可枚举结构体。
    /// </summary>
    /// <param name="source">源字符串视图。</param>
    /// <param name="separator">分隔字符。</param>
    public StringViewEnumerable(StringView source, char separator)
    {
        _source = source;
        _separator = separator;
    }

    /// <summary>
    /// 获取枚举器。
    /// </summary>
    /// <returns>字符串视图枚举器。</returns>
    public StringViewEnumerator GetEnumerator()
    {
        return new StringViewEnumerator(_source, _separator);
    }
}

/// <summary>
/// 用于迭代 StringView.Split 结果的枚举器结构体。
/// </summary>
public ref struct StringViewEnumerator
{
    private readonly StringView _source;
    private readonly char _separator;
    private int _position;
    private StringView _current;

    /// <summary>
    /// 获取当前元素。
    /// </summary>
    public StringView Current => _current;

    /// <summary>
    /// 使用源字符串视图和分隔符初始化枚举器。
    /// </summary>
    /// <param name="source">源字符串视图。</param>
    /// <param name="separator">分隔字符。</param>
    public StringViewEnumerator(StringView source, char separator)
    {
        _source = source;
        _separator = separator;
        _position = 0;
        _current = default;
    }

    /// <summary>
    /// 将枚举器推进到下一个元素。
    /// </summary>
    /// <returns>如果还有元素返回 true，否则返回 false。</returns>
    public bool MoveNext()
    {
        if (_position > _source.Length)
        {
            return false;
        }

        var start = _position;
        var idx = _source.IndexOf(_separator, _position);

        if (idx < 0)
        {
            _current = _source.Substring(start);
            _position = _source.Length + 1;
        }
        else
        {
            _current = _source.Substring(start, idx - start);
            _position = idx + 1;
        }

        return true;
    }
}
