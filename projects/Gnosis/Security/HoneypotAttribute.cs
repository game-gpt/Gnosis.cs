namespace Gnosis.Security;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class HoneypotAttribute : Attribute
{
    public string TriggerEvent { get; set; } = "on_cheat_suspected";
    
    public HoneypotAttribute() { }
}
