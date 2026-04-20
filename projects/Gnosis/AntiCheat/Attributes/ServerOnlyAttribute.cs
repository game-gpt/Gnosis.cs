namespace Gnosis.AntiCheat.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class ServerOnlyAttribute : Attribute
{
    public ServerOnlyAttribute() { }
}
