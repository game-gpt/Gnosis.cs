namespace Gnosis.Security;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property)]
public class EncryptedAttribute : Attribute
{
    public EncryptedAttribute() { }
}
