namespace Gnosis.Graphic.RHI;

/// <summary>
/// 模板缓冲操作
/// </summary>
public enum StencilOp
{
    Keep = 0,
    Zero = 1,
    Replace = 2,
    IncrementClamp = 3,
    DecrementClamp = 4,
    Invert = 5,
    IncrementWrap = 6,
    DecrementWrap = 7
}
