using System;

namespace Gnosis.Network;

/// <summary>
/// 确定性随机数生成器，确保不同客户端产生相同的随机序列
/// </summary>
public sealed class DeterministicRandom
{
    #region 字段

    private int _state;

    #endregion

    #region 属性

    /// <summary>
    /// 获取当前种子
    /// </summary>
    public int Seed { get; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化确定性随机数生成器
    /// </summary>
    /// <param name="seed">随机种子</param>
    public DeterministicRandom(int seed)
    {
        Seed = seed;
        _state = seed;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 生成 [min, max) 范围内的确定性随机整数
    /// </summary>
    /// <param name="min">最小值（含）</param>
    /// <param name="max">最大值（不含）</param>
    /// <returns>随机整数</returns>
    public int NextInt(int min, int max)
    {
        if (min >= max)
        {
            throw new ArgumentException($"最小值 {min} 必须小于最大值 {max}");
        }

        return min + Math.Abs(Next()) % (max - min);
    }

    /// <summary>
    /// 生成 [0, 1) 范围内的确定性随机浮点数
    /// </summary>
    /// <returns>随机浮点数</returns>
    public float NextFloat()
    {
        return (float)(Math.Abs(Next()) / 2147483648.0);
    }

    /// <summary>
    /// 克隆当前随机状态
    /// </summary>
    /// <returns>具有相同状态的副本</returns>
    public DeterministicRandom Clone()
    {
        var clone = new DeterministicRandom(Seed);
        clone._state = _state;
        return clone;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 生成下一个随机整数（使用线性同余法）
    /// </summary>
    /// <returns>随机整数</returns>
    private int Next()
    {
        _state = unchecked(_state * 1103515245 + 12345);
        return _state;
    }

    #endregion
}
