namespace Gnosis.Security.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ServerOnlyAttribute : Attribute
{
    public ServerOnlyAttribute() { }
}
