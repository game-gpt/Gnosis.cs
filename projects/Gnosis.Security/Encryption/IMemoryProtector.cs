namespace Gnosis.Security.Encryption;

public interface IMemoryProtector
{
    byte[] Encrypt(byte[] data);
    byte[] Decrypt(byte[] encryptedData);

    T Obfuscate<T>(T value) where T : struct;
    T Deobfuscate<T>(T obfuscatedValue) where T : struct;

    byte[] GenerateHoneypot();
}
