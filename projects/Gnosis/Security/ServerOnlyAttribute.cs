namespace Gnosis.Security;

[AttributeUsage(AttributeTargets.Method)]
public class ServerOnlyAttribute : Attribute
{
    public ServerOnlyAttribute() { }
}
