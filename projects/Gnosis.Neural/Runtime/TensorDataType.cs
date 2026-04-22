namespace Gnosis.Neural.Runtime;

/// <summary>
/// 张量数据类型
/// </summary>
public enum TensorDataType : byte
{
    Float32 = 0,
    Float16 = 1,
    BFloat16 = 2,
    Int8 = 3,
    Int32 = 4,
    UInt8 = 5
}
