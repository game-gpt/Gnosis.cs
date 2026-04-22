using Gnosis.Core.Time;

namespace Gnosis.Security.Integrity;

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

    public string Name { get; }
    public Timestamp LastCheckTime { get; private set; }

    #endregion

    #region 构造函数

    public IntegrityChecker(string name, byte[] originalBytes, Func<byte[]> currentBytesProvider)
    {
        Name = name;
        _originalHash = ComputeCrc32(originalBytes);
        _currentBytesProvider = currentBytesProvider;
        LastCheckTime = default;
    }

    #endregion

    #region 公开方法

    public bool Check()
    {
        var currentBytes = _currentBytesProvider();
        var currentHash = ComputeCrc32(currentBytes);
        LastCheckTime = Timestamp.Now;
        return currentHash == _originalHash;
    }

    #endregion

    #region 私有方法

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
