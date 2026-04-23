using Gnosis.Core.Exceptions;

namespace Gnosis.AI.Exceptions;

/// <summary>
///     AI 感知配置不支持异常，当传入不支持的感知配置类型时抛出
/// </summary>
public sealed class AISenseConfigNotSupportedException : GnosisException
{
    public AISenseConfigNotSupportedException(string configTypeName)
        : base("AI_001", $"不支持的感知配置类型: {configTypeName}")
    {
    }
}

/// <summary>
///     AI 行为树异常
/// </summary>
public sealed class BehaviorTreeException : GnosisException
{
    public BehaviorTreeException(string message)
        : base("AI_002", message)
    {
    }
}

/// <summary>
///     AI 规划异常
/// </summary>
public sealed class AIPlanningException : GnosisException
{
    public AIPlanningException(string message)
        : base("AI_003", message)
    {
    }
}
