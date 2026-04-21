namespace Gnosis.Security.Encrypted;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Field | AttributeTargets.Property)]
public class EncryptedAttribute : Attribute
{
    public EncryptedAttribute() { }
}
