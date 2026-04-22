namespace Gnosis.Network.Replication;

/// <summary>
/// 标记需要网络复制的属性或字段
/// </summary>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property)]
public class ReplicatedAttribute : Attribute
{
    public string Reliability { get; set; } = "reliable";

    public ReplicatedAttribute() { }
}
