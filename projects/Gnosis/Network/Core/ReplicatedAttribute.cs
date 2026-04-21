namespace Gnosis.Network.Core;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property)]
public class ReplicatedAttribute : Attribute
{
    public string Reliability { get; set; } = "reliable";

    public ReplicatedAttribute() { }
}
