using System.Diagnostics;

namespace Gnosis.Runtime.Sandbox;

public sealed class Sandbox : IDisposable
{
    #region Fields

    private readonly string _scriptId;
    private readonly ScriptPermissionSet _permissions;
    private readonly ResourceQuota _quota;
    private readonly Dictionary<string, object> _isolatedStorage;
    private readonly HashSet<string> _allowedNativeFunctions;
    private readonly HashSet<string> _deniedNativeFunctions;
    private readonly List<SandboxViolationRecord> _violationLog;
    private readonly object _lock;
    private bool _isDisposed;
    private bool _isActive;

    #endregion

    #region Properties

    public string ScriptId => _scriptId;
    public bool IsActive => _isActive && !_isDisposed;
    public ScriptPermissionSet Permissions => _permissions;
    public ResourceQuota Quota => _quota;
    public IReadOnlyList<SandboxViolationRecord> ViolationLog => _violationLog;
    public int ViolationCount => _violationLog.Count;

    #endregion

    #region Constructors

    public Sandbox(string scriptId)
        : this(scriptId, ScriptPermissionSet.CreateDefault(), ResourceQuota.Default())
    {
    }

    public Sandbox(string scriptId, ScriptPermissionSet permissions, ResourceQuota quota)
    {
        _scriptId = scriptId ?? throw new ArgumentNullException(nameof(scriptId));
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
        _quota = quota ?? throw new ArgumentNullException(nameof(quota));
        _isolatedStorage = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        _allowedNativeFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _deniedNativeFunctions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _violationLog = new List<SandboxViolationRecord>();
        _lock = new object();
        _isActive = true;
        _isDisposed = false;
    }

    #endregion

    #region Permission Checks

    public bool CheckPermission(ScriptPermission permission)
    {
        ThrowIfDisposed();
        ThrowIfNotActive();

        return _permissions.Has(permission);
    }

    public void DemandPermission(ScriptPermission permission)
    {
        ThrowIfDisposed();
        ThrowIfNotActive();

        if (!_permissions.Has(permission))
        {
            LogViolation(ViolationType.PermissionDenied, $"脚本 {_scriptId} 缺少权限：{permission}");
            throw new SandboxViolationException(_scriptId, ViolationType.PermissionDenied,
                $"脚本 {_scriptId} 缺少权限：{permission}");
        }
    }

    public bool CanCallNativeFunction(string functionName)
    {
        ThrowIfDisposed();
        ThrowIfNotActive();

        if (!_permissions.Has(ScriptPermission.NativeCall))
        {
            return false;
        }

        if (_deniedNativeFunctions.Contains(functionName))
        {
            return false;
        }

        if (_allowedNativeFunctions.Count > 0 && !_allowedNativeFunctions.Contains(functionName))
        {
            return false;
        }

        return true;
    }

    public void DemandNativeCall(string functionName)
    {
        if (!CanCallNativeFunction(functionName))
        {
            LogViolation(ViolationType.PermissionDenied, $"脚本 {_scriptId} 无权调用原生函数：{functionName}");
            throw new SandboxViolationException(_scriptId, ViolationType.PermissionDenied,
                $"脚本 {_scriptId} 无权调用原生函数：{functionName}");
        }
    }

    #endregion

    #region Resource Quota Checks

    public bool TryAllocateMemory(long size)
    {
        ThrowIfDisposed();
        ThrowIfNotActive();

        if (!_quota.CanAllocateMemory(size))
        {
            LogViolation(ViolationType.MemoryQuotaExceeded,
                $"脚本 {_scriptId} 内存配额不足：请求 {size} 字节，已使用 {_quota.CurrentMemoryUsage} 字节");
            return false;
        }

        _quota.RecordAllocation(size);
        return true;
    }

    public void DemandAllocateMemory(long size)
    {
        if (!TryAllocateMemory(size))
        {
            throw new SandboxViolationException(_scriptId, ViolationType.MemoryQuotaExceeded,
                $"脚本 {_scriptId} 内存配额不足：请求 {size} 字节");
        }
    }

    public bool TryCreateObject()
    {
        ThrowIfDisposed();
        ThrowIfNotActive();

        if (!_quota.CanCreateObject())
        {
            LogViolation(ViolationType.ObjectQuotaExceeded,
                $"脚本 {_scriptId} 对象配额不足：已创建 {_quota.CurrentObjectCount} 个");
            return false;
        }

        _quota.RecordObjectCreated();
        return true;
    }

    public void DemandCreateObject()
    {
        if (!TryCreateObject())
        {
            throw new SandboxViolationException(_scriptId, ViolationType.ObjectQuotaExceeded,
                $"脚本 {_scriptId} 对象配额不足");
        }
    }

    public bool TryExecuteInstruction()
    {
        if (!_quota.CanExecuteInstruction())
        {
            return false;
        }

        _quota.RecordInstructionExecuted();
        return true;
    }

    public void DemandExecuteInstruction()
    {
        if (!_quota.CanExecuteInstruction())
        {
            LogViolation(ViolationType.InstructionQuotaExceeded,
                $"脚本 {_scriptId} 指令配额不足：已执行 {_quota.CurrentInstructionCount} 条");
            throw new SandboxViolationException(_scriptId, ViolationType.InstructionQuotaExceeded,
                $"脚本 {_scriptId} 指令配额不足");
        }

        _quota.RecordInstructionExecuted();
    }

    public bool TryPushFrame(int currentDepth)
    {
        if (!_quota.CanPushFrame(currentDepth))
        {
            return false;
        }

        return true;
    }

    public void DemandPushFrame(int currentDepth)
    {
        if (!_quota.CanPushFrame(currentDepth))
        {
            LogViolation(ViolationType.StackDepthExceeded,
                $"脚本 {_scriptId} 栈深度超限：当前 {currentDepth}");
            throw new SandboxViolationException(_scriptId, ViolationType.StackDepthExceeded,
                $"脚本 {_scriptId} 栈深度超限：当前 {currentDepth}");
        }
    }

    public bool TryCreateCoroutine()
    {
        ThrowIfDisposed();
        ThrowIfNotActive();

        if (!_quota.CanCreateCoroutine())
        {
            LogViolation(ViolationType.CoroutineQuotaExceeded,
                $"脚本 {_scriptId} 协程配额不足：已创建 {_quota.CurrentCoroutineCount} 个");
            return false;
        }

        _quota.RecordCoroutineCreated();
        return true;
    }

    public void DemandCreateCoroutine()
    {
        if (!TryCreateCoroutine())
        {
            throw new SandboxViolationException(_scriptId, ViolationType.CoroutineQuotaExceeded,
                $"脚本 {_scriptId} 协程配额不足");
        }
    }

    public void CheckExecutionTime()
    {
        if (!_quota.IsWithinTimeLimit())
        {
            LogViolation(ViolationType.ExecutionTimeExceeded,
                $"脚本 {_scriptId} 执行时间超限：已执行 {_quota.ElapsedExecutionTime}");
            throw new SandboxViolationException(_scriptId, ViolationType.ExecutionTimeExceeded,
                $"脚本 {_scriptId} 执行时间超限");
        }
    }

    #endregion

    #region Native Function Access Control

    public void AllowNativeFunction(string functionName)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(functionName))
        {
            return;
        }

        _allowedNativeFunctions.Add(functionName);
        _deniedNativeFunctions.Remove(functionName);
    }

    public void DenyNativeFunction(string functionName)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(functionName))
        {
            return;
        }

        _deniedNativeFunctions.Add(functionName);
        _allowedNativeFunctions.Remove(functionName);
    }

    #endregion

    #region Isolated Storage

    public void SetData(string key, object value)
    {
        ThrowIfDisposed();
        ThrowIfNotActive();

        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _isolatedStorage[key] = value;
    }

    public object? GetData(string key)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        return _isolatedStorage.GetValueOrDefault(key);
    }

    public T? GetData<T>(string key)
    {
        var value = GetData(key);

        if (value is T typed)
        {
            return typed;
        }

        return default;
    }

    public bool RemoveData(string key)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return _isolatedStorage.Remove(key);
    }

    #endregion

    #region Lifecycle

    public void Suspend()
    {
        ThrowIfDisposed();
        _quota.StopTiming();
        _isActive = false;
    }

    public void Resume()
    {
        ThrowIfDisposed();
        _isActive = true;
        _quota.StartTiming();
    }

    public void ResetQuotas()
    {
        ThrowIfDisposed();
        _quota.Reset();
    }

    #endregion

    #region Violation Logging

    private void LogViolation(ViolationType type, string message)
    {
        lock (_lock)
        {
            _violationLog.Add(new SandboxViolationRecord(
                _scriptId,
                type,
                message,
                DateTime.UtcNow
            ));
        }
    }

    public void ClearViolationLog()
    {
        lock (_lock)
        {
            _violationLog.Clear();
        }
    }

    #endregion

    #region Private Methods

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(Sandbox));
        }
    }

    private void ThrowIfNotActive()
    {
        if (!_isActive)
        {
            throw new SandboxViolationException(_scriptId, ViolationType.PermissionDenied,
                $"脚本 {_scriptId} 的沙箱已挂起，无法执行操作");
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _quota.StopTiming();
        _isolatedStorage.Clear();
        _allowedNativeFunctions.Clear();
        _deniedNativeFunctions.Clear();
        _violationLog.Clear();
        _isActive = false;
        _isDisposed = true;
    }

    #endregion
}

public sealed record SandboxViolationRecord(
    string ScriptId,
    ViolationType ViolationType,
    string Message,
    DateTime Timestamp
);
