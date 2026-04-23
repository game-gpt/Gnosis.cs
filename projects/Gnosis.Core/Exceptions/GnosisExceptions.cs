namespace Gnosis.Core.Exceptions;

/// <summary>
///     Gnosis 框架异常基类，所有领域专用异常的父类
/// </summary>
public abstract class GnosisException : Exception
{
    /// <summary>
    ///     错误代码，用于程序化识别错误类型
    /// </summary>
    public string ErrorCode { get; }

    protected GnosisException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    protected GnosisException(string errorCode, string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}

/// <summary>
///     服务未连接异常，当尝试使用未连接的服务时抛出
/// </summary>
public sealed class ServiceNotConnectedException : GnosisException
{
    public ServiceNotConnectedException(string serviceName)
        : base("GNOSIS_SVC_001", $"{serviceName}未连接")
    {
    }
}

/// <summary>
///     服务已连接异常，当重复连接已连接的服务时抛出
/// </summary>
public sealed class ServiceAlreadyConnectedException : GnosisException
{
    public ServiceAlreadyConnectedException(string serviceName)
        : base("GNOSIS_SVC_002", $"{serviceName}已连接")
    {
    }
}

/// <summary>
///     操作无效异常，当在当前状态下执行不允许的操作时抛出
/// </summary>
public sealed class InvalidOperationException : GnosisException
{
    public InvalidOperationException(string message)
        : base("GNOSIS_OPR_001", message)
    {
    }
}

/// <summary>
///     资源不存在异常，当访问不存在的资源时抛出
/// </summary>
public sealed class ResourceNotFoundException : GnosisException
{
    public ResourceNotFoundException(string resourceType, string resourceId)
        : base("GNOSIS_RES_001", $"{resourceType} '{resourceId}' 不存在")
    {
    }
}

/// <summary>
///     资源已满异常，当资源容量达到上限时抛出
/// </summary>
public sealed class ResourceFullException : GnosisException
{
    public ResourceFullException(string resourceType)
        : base("GNOSIS_RES_002", $"{resourceType}已满")
    {
    }
}

/// <summary>
///     资源已存在异常，当创建已存在的资源时抛出
/// </summary>
public sealed class ResourceAlreadyExistsException : GnosisException
{
    public ResourceAlreadyExistsException(string resourceType, string resourceId)
        : base("GNOSIS_RES_003", $"{resourceType} '{resourceId}' 已存在")
    {
    }
}

/// <summary>
///     权限不足异常，当执行无权限的操作时抛出
/// </summary>
public sealed class PermissionDeniedException : GnosisException
{
    public PermissionDeniedException(string operation)
        : base("GNOSIS_SEC_001", $"无权限执行操作: {operation}")
    {
    }
}

/// <summary>
///     配置无效异常，当配置参数不合法时抛出
/// </summary>
public sealed class InvalidConfigurationException : GnosisException
{
    public InvalidConfigurationException(string parameterName, string reason)
        : base("GNOSIS_CFG_001", $"配置参数 '{parameterName}' 无效: {reason}")
    {
    }
}
