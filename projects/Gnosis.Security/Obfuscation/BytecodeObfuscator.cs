using Gnosis.Security.AntiCheat;

namespace Gnosis.Security.Obfuscation;

public sealed class BytecodeObfuscator
{
    #region 属性

    public bool IsIsaRandomizationEnabled { get; private set; }
    public bool IsControlFlowFlatteningEnabled { get; private set; }
    public bool IsStringEncryptionEnabled { get; private set; }

    #endregion

    #region 公开方法

    public void EnableIsaRandomization() => IsIsaRandomizationEnabled = true;

    public void EnableControlFlowFlattening() => IsControlFlowFlatteningEnabled = true;

    public void EnableStringEncryption() => IsStringEncryptionEnabled = true;

    public byte[] Obfuscate(byte[] bytecode)
    {
        if (bytecode is null || bytecode.Length == 0) throw new SecurityException("混淆字节码不能为空");
        return bytecode;
    }

    public int GenerateIsaSeed() => Random.Shared.Next();

    #endregion
}
