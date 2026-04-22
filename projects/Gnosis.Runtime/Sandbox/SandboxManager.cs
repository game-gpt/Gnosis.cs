namespace Gnosis.Runtime.Sandbox;

public sealed class SandboxManager : IDisposable
{
    #region Fields

    private readonly Dictionary<string, Sandbox> _sandboxes;
    private readonly SandboxPolicy _defaultPolicy;
    private bool _isDisposed;

    #endregion

    #region Properties

    public int ActiveSandboxCount => _sandboxes.Count(s => s.Value.IsActive);
    public int TotalSandboxCount => _sandboxes.Count;
    public SandboxPolicy DefaultPolicy => _defaultPolicy;

    #endregion

    #region Constructors

    public SandboxManager()
        : this(SandboxPolicy.Default)
    {
    }

    public SandboxManager(SandboxPolicy defaultPolicy)
    {
        _defaultPolicy = defaultPolicy ?? throw new ArgumentNullException(nameof(defaultPolicy));
        _sandboxes = new Dictionary<string, Sandbox>(StringComparer.OrdinalIgnoreCase);
        _isDisposed = false;
    }

    #endregion

    #region Sandbox Creation

    public Sandbox CreateSandbox(string scriptId)
    {
        return CreateSandbox(scriptId, _defaultPolicy);
    }

    public Sandbox CreateSandbox(string scriptId, SandboxPolicy policy)
    {
        ThrowIfDisposed();

        if (string.IsNullOrWhiteSpace(scriptId))
        {
            throw new ArgumentException("脚本标识不能为空", nameof(scriptId));
        }

        if (_sandboxes.ContainsKey(scriptId))
        {
            throw new InvalidOperationException($"沙箱已存在：{scriptId}");
        }

        var permissions = policy.PermissionSet ?? ScriptPermissionSet.CreateDefault();
        var quota = policy.Quota ?? ResourceQuota.Default();

        var sandbox = new Sandbox(scriptId, permissions, quota);

        if (policy.AutoStart)
        {
            sandbox.Resume();
        }

        _sandboxes[scriptId] = sandbox;
        return sandbox;
    }

    public bool TryGetSandbox(string scriptId, out Sandbox? sandbox)
    {
        ThrowIfDisposed();
        return _sandboxes.TryGetValue(scriptId, out sandbox);
    }

    public Sandbox GetOrCreateSandbox(string scriptId)
    {
        if (TryGetSandbox(scriptId, out var existing))
        {
            return existing!;
        }

        return CreateSandbox(scriptId);
    }

    #endregion

    #region Sandbox Destruction

    public bool DestroySandbox(string scriptId)
    {
        ThrowIfDisposed();

        if (!_sandboxes.TryGetValue(scriptId, out var sandbox))
        {
            return false;
        }

        sandbox.Dispose();
        _sandboxes.Remove(scriptId);
        return true;
    }

    public void DestroyAllSandboxes()
    {
        ThrowIfDisposed();

        foreach (var sandbox in _sandboxes.Values)
        {
            sandbox.Dispose();
        }

        _sandboxes.Clear();
    }

    #endregion

    #region Bulk Operations

    public void SuspendAll()
    {
        ThrowIfDisposed();

        foreach (var sandbox in _sandboxes.Values)
        {
            if (sandbox.IsActive)
            {
                sandbox.Suspend();
            }
        }
    }

    public void ResumeAll()
    {
        ThrowIfDisposed();

        foreach (var sandbox in _sandboxes.Values)
        {
            if (!sandbox.IsActive)
            {
                sandbox.Resume();
            }
        }
    }

    public void ResetAllQuotas()
    {
        ThrowIfDisposed();

        foreach (var sandbox in _sandboxes.Values)
        {
            sandbox.ResetQuotas();
        }
    }

    #endregion

    #region Violation Aggregation

    public IReadOnlyList<SandboxViolationRecord> GetAllViolations()
    {
        var all = new List<SandboxViolationRecord>();

        foreach (var sandbox in _sandboxes.Values)
        {
            all.AddRange(sandbox.ViolationLog);
        }

        return all.OrderBy(v => v.Timestamp).ToList();
    }

    public int GetTotalViolationCount()
    {
        return _sandboxes.Values.Sum(s => s.ViolationCount);
    }

    #endregion

    #region Private Methods

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(SandboxManager));
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

        DestroyAllSandboxes();
        _isDisposed = true;
    }

    #endregion
}

public sealed class SandboxPolicy
{
    public ScriptPermissionSet? PermissionSet { get; init; }
    public ResourceQuota? Quota { get; init; }
    public bool AutoStart { get; init; } = true;

    public static SandboxPolicy Default => new()
    {
        PermissionSet = ScriptPermissionSet.CreateDefault(),
        Quota = ResourceQuota.Default(),
        AutoStart = true
    };

    public static SandboxPolicy Strict => new()
    {
        PermissionSet = ScriptPermissionSet.CreateStrict(),
        Quota = ResourceQuota.Strict(),
        AutoStart = true
    };

    public static SandboxPolicy Permissive => new()
    {
        PermissionSet = ScriptPermissionSet.CreateFullAccess(),
        Quota = ResourceQuota.Unlimited(),
        AutoStart = true
    };
}
