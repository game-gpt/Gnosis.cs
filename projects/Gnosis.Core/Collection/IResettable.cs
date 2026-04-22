namespace Gnosis.Core.Collection;

/// <summary>
/// 可重置对象接口，用于对象池回收时重置状态
/// </summary>
public interface IResettable
{
    /// <summary>
    /// 重置对象状态
    /// </summary>
    void Reset();
}
