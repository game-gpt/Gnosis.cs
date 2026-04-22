namespace Gnosis.Runtime.Sandbox;

public class SandboxViolationException : Exception
{
    public string ScriptId { get; }
    public ViolationType ViolationType { get; }

    public SandboxViolationException(string scriptId, string message)
        : base(message)
    {
        ScriptId = scriptId;
        ViolationType = ViolationType.Unknown;
    }

    public SandboxViolationException(string scriptId, ViolationType violationType, string message)
        : base(message)
    {
        ScriptId = scriptId;
        ViolationType = violationType;
    }

    public SandboxViolationException(string scriptId, ViolationType violationType, string message, Exception innerException)
        : base(message, innerException)
    {
        ScriptId = scriptId;
        ViolationType = violationType;
    }
}

public enum ViolationType
{
    Unknown,
    PermissionDenied,
    MemoryQuotaExceeded,
    ObjectQuotaExceeded,
    InstructionQuotaExceeded,
    StackDepthExceeded,
    CoroutineQuotaExceeded,
    ExecutionTimeExceeded,
    StringSizeExceeded,
    ArrayLengthExceeded
}
