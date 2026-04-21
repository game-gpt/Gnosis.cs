namespace Gnosis.Security;

[AttributeUsage(AttributeTargets.Method)]
public class SensitiveAttribute : Attribute
{
    public string CheckFrequency { get; set; } = "once_per_session";
    
    public SensitiveAttribute() { }
}
