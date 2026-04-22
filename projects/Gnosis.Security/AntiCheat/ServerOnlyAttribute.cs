namespace Gnosis.Security.AntiCheat;

[AttributeUsage(AttributeTargets.Method)]
public class ServerOnlyAttribute : Attribute
{
    public ServerOnlyAttribute() { }
}
