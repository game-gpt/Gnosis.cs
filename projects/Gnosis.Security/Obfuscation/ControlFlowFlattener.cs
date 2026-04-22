using Gnosis.Security.AntiCheat;

namespace Gnosis.Security.Obfuscation;

public sealed class ControlFlowFlattener
{
    #region 属性

    public bool IsBogusControlFlowEnabled { get; private set; }
    public bool IsOpaquePredicateEnabled { get; private set; }

    #endregion

    #region 公开方法

    public void EnableBogusControlFlow() => IsBogusControlFlowEnabled = true;

    public void EnableOpaquePredicate() => IsOpaquePredicateEnabled = true;

    public byte[] Flatten(byte[] bytecode)
    {
        if (bytecode is null || bytecode.Length == 0) throw new SecurityException("字节码不能为空");
        return bytecode;
    }

    #endregion
}
