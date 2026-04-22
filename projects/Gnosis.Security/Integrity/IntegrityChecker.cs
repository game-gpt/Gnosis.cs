using Gnosis.Core.Hash;
using Gnosis.Core.Time;

namespace Gnosis.Security.Integrity;

public sealed class IntegrityChecker : IIntegrityChecker
{
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
        _originalHash = Crc32.Compute(originalBytes);
        _currentBytesProvider = currentBytesProvider;
        LastCheckTime = default;
    }

    #endregion

    #region 公开方法

    public bool Check()
    {
        var currentBytes = _currentBytesProvider();
        var currentHash = Crc32.Compute(currentBytes);
        LastCheckTime = Timestamp.Now;
        return currentHash == _originalHash;
    }

    #endregion
}
