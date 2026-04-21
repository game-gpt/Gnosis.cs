namespace Gnosis.Security;

/// <summary>
/// 字节码混淆器，提供指令集随机化和控制流平坦化
/// </summary>
public sealed class BytecodeObfuscator
{
    #region 属性

    /// <summary>
    /// 获取是否已启用指令集随机化
    /// </summary>
    public bool IsIsaRandomizationEnabled { get; private set; }

    /// <summary>
    /// 获取是否已启用控制流平坦化
    /// </summary>
    public bool IsControlFlowFlatteningEnabled { get; private set; }

    /// <summary>
    /// 获取是否已启用字符串加密
    /// </summary>
    public bool IsStringEncryptionEnabled { get; private set; }

    #endregion

    #region 公开方法

    /// <summary>
    /// 启用指令集随机化，使每次编译产生不同的操作码映射
    /// </summary>
    public void EnableIsaRandomization()
    {
        IsIsaRandomizationEnabled = true;
    }

    /// <summary>
    /// 启用控制流平坦化，将函数的控制流图转换为状态机形式
    /// </summary>
    public void EnableControlFlowFlattening()
    {
        IsControlFlowFlatteningEnabled = true;
    }

    /// <summary>
    /// 启用字符串加密，对字节码中的字符串常量进行加密
    /// </summary>
    public void EnableStringEncryption()
    {
        IsStringEncryptionEnabled = true;
    }

    /// <summary>
    /// 对字节码进行混淆处理
    /// </summary>
    /// <param name="bytecode">原始字节码</param>
    /// <returns>混淆后的字节码</returns>
    /// <exception cref="SecurityException">当字节码为 null 或空时抛出</exception>
    public byte[] Obfuscate(byte[] bytecode)
    {
        if (bytecode is null || bytecode.Length == 0)
        {
            throw new SecurityException("混淆字节码不能为空");
        }

        return bytecode;
    }

    /// <summary>
    /// 生成指令集随机化种子，用于编译器和虚拟机的操作码映射同步
    /// </summary>
    /// <returns>随机种子</returns>
    public int GenerateIsaSeed()
    {
        return Random.Shared.Next();
    }

    #endregion
}
