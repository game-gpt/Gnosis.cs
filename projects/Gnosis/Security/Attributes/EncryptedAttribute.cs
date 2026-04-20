namespace Gnosis.Security.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class EncryptedAttribute : Attribute
{
    public EncryptedAttribute() { }
}
