namespace Gnosis.Neural.Model;

/// <summary>
/// 量化类型
/// </summary>
public enum QuantizationType : byte
{
    None = 0,
    Float16 = 1,
    Int8 = 2,
    BFloat16 = 3
}
