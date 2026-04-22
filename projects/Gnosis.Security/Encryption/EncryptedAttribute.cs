namespace Gnosis.Security.Encryption;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property)]
public class EncryptedAttribute : Attribute
{
    public EncryptedAttribute() { }
}
