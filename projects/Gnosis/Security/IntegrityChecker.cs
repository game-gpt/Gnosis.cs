using Gnosis.Infrastructure;

namespace Gnosis.Security;

/// <summary>
/// 完整性检查器，通过 CRC32 哈希比对检测字节码是否被篡改
/// </summary>
public sealed class IntegrityChecker : IIntegrityChecker
{
    #region 常量

    private const uint Crc32Polynomial = 0xEDB88320;

    #endregion

    #region 静态字段

    private static readonly uint[] Crc32Table = BuildCrc32Table();

    #endregion

    #region 实例字段

    private readonly uint _originalHash;
    private readonly Func<byte[]> _currentBytesProvider;

    #endregion

    #region 属性

    /// <summary>
    /// 检查器名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 上次检查的时间戳
    /// </summary>
    public Timestamp LastCheckTime { get; private set; }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化完整性检查器
    /// </summary>
    /// <param name="name">检查器名称</param>
    /// <param name="originalBytes">原始方法字节码</param>
    /// <param name="currentBytesProvider">用于获取当前字节码的委托</param>
    public IntegrityChecker(string name, byte[] originalBytes, Func<byte[]> currentBytesProvider)
    {
        Name = name;
        _originalHash = ComputeCrc32(originalBytes);
        _currentBytesProvider = currentBytesProvider;
        LastCheckTime = default;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 执行完整性检查，比较当前字节码的 CRC32 哈希与原始哈希
    /// </summary>
    /// <returns>若哈希一致返回 true，否则返回 false</returns>
    public bool Check()
    {
        var currentBytes = _currentBytesProvider();
        var currentHash = ComputeCrc32(currentBytes);
        LastCheckTime = Timestamp.Now;
        return currentHash == _originalHash;
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 构建 CRC32 查找表
    /// </summary>
    private static uint[] BuildCrc32Table()
    {
        var table = new uint[256];

        for (var i = 0; i < 256; i++)
        {
            var crc = (uint)i;

            for (var bit = 0; bit < 8; bit++)
            {
                if ((crc & 1) != 0)
                {
                    crc = (crc >> 1) ^ Crc32Polynomial;
                }
                else
                {
                    crc >>= 1;
                }
            }

            table[i] = crc;
        }

        return table;
    }

    /// <summary>
    /// 计算给定字节数组的 CRC32 哈希值
    /// </summary>
    /// <param name="bytes">待计算的字节数组</param>
    /// <returns>CRC32 哈希值</returns>
    private static uint ComputeCrc32(byte[] bytes)
    {
        var crc = 0xFFFFFFFF;

        foreach (var b in bytes)
        {
            var index = (crc ^ b) & 0xFF;
            crc = (crc >> 8) ^ Crc32Table[index];
        }

        return crc ^ 0xFFFFFFFF;
    }

    #endregion
}
