namespace Gnosis.Security;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class EncryptedAttribute : Attribute
{
    public EncryptedAttribute() { }
}
