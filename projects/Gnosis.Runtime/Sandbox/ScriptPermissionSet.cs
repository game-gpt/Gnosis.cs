namespace Gnosis.Runtime.Sandbox;

public sealed class ScriptPermissionSet
{
    private ScriptPermission _granted;

    public string Name { get; }
    public ScriptPermission Granted => _granted;

    public ScriptPermissionSet(string name)
    {
        Name = name;
        _granted = ScriptPermission.None;
    }

    public bool Has(ScriptPermission permission)
    {
        return (_granted & permission) == permission;
    }

    public void Grant(ScriptPermission permission)
    {
        _granted |= permission;
    }

    public void Revoke(ScriptPermission permission)
    {
        _granted &= ~permission;
    }

    public void Demand(ScriptPermission permission)
    {
        if (!Has(permission))
        {
            throw new SandboxViolationException(Name, $"脚本缺少必要权限：{permission}");
        }
    }

    public static ScriptPermissionSet CreateDefault()
    {
        var set = new ScriptPermissionSet("default");
        set.Grant(ScriptPermission.NativeCall);
        set.Grant(ScriptPermission.EntityAccess);
        set.Grant(ScriptPermission.ComponentCreate);
        set.Grant(ScriptPermission.ComponentDestroy);
        set.Grant(ScriptPermission.QueryExecution);
        set.Grant(ScriptPermission.GlobalVariableRead);
        set.Grant(ScriptPermission.GlobalVariableWrite);
        set.Grant(ScriptPermission.CoroutineCreate);
        return set;
    }

    public static ScriptPermissionSet CreateStrict()
    {
        var set = new ScriptPermissionSet("strict");
        set.Grant(ScriptPermission.NativeCall);
        return set;
    }

    public static ScriptPermissionSet CreateFullAccess()
    {
        var set = new ScriptPermissionSet("full");
        set._granted = ScriptPermission.All;
        return set;
    }
}
