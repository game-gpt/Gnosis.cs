using System;
using Gnosis.Security;
using NUnit.Framework;

namespace Gnosis.Security.Tests;

/// <summary>
/// MemoryEncryptor 单元测试，验证基于 XOR 的内存加密、解密、混淆及蜜罐生成功能
/// </summary>
[TestFixture]
public class MemoryEncryptorTests
{
    #region 字段

    private MemoryEncryptor _encryptor = null!;

    #endregion

    #region 初始化

    [SetUp]
    public void Setup()
    {
        _encryptor = new MemoryEncryptor();
    }

    #endregion

    #region Encrypt 测试

    [Test]
    public void Encrypt_WithValidData_ReturnsDifferentBytes()
    {
        byte[] data = { 1, 2, 3, 4, 5 };

        byte[] encrypted = _encryptor.Encrypt(data);

        Assert.That(encrypted, Is.Not.Null);
        Assert.That(encrypted, Is.Not.EqualTo(data));
    }

    [Test]
    public void Encrypt_NullData_ThrowsSecurityException()
    {
        Assert.Throws<SecurityException>(() => _encryptor.Encrypt(null!));
    }

    [Test]
    public void Encrypt_EmptyData_ThrowsSecurityException()
    {
        Assert.Throws<SecurityException>(() => _encryptor.Encrypt(Array.Empty<byte>()));
    }

    #endregion

    #region Decrypt 测试

    [Test]
    public void Decrypt_WithEncryptedData_ReturnsOriginalData()
    {
        byte[] data = { 10, 20, 30, 40, 50 };

        byte[] encrypted = _encryptor.Encrypt(data);
        byte[] decrypted = _encryptor.Decrypt(encrypted);

        Assert.That(decrypted, Is.EqualTo(data));
    }

    [Test]
    public void Decrypt_NullData_ThrowsSecurityException()
    {
        Assert.Throws<SecurityException>(() => _encryptor.Decrypt(null!));
    }

    [Test]
    public void Decrypt_EmptyData_ThrowsSecurityException()
    {
        Assert.Throws<SecurityException>(() => _encryptor.Decrypt(Array.Empty<byte>()));
    }

    #endregion

    #region Obfuscate 测试

    [Test]
    public void Obfuscate_Int_ReturnsDifferentValue()
    {
        int original = 12345;

        int obfuscated = _encryptor.Obfuscate(original);

        Assert.That(obfuscated, Is.Not.EqualTo(original));
    }

    [Test]
    public void Obfuscate_Float_ReturnsDifferentValue()
    {
        float original = 3.14f;

        float obfuscated = _encryptor.Obfuscate(original);

        Assert.That(obfuscated, Is.Not.EqualTo(original));
    }

    [Test]
    public void Obfuscate_Long_ReturnsDifferentValue()
    {
        long original = 9876543210L;

        long obfuscated = _encryptor.Obfuscate(original);

        Assert.That(obfuscated, Is.Not.EqualTo(original));
    }

    #endregion

    #region Deobfuscate 测试

    [Test]
    public void Deobfuscate_Int_ReturnsOriginalValue()
    {
        int original = 12345;

        int obfuscated = _encryptor.Obfuscate(original);
        int deobfuscated = _encryptor.Deobfuscate(obfuscated);

        Assert.That(deobfuscated, Is.EqualTo(original));
    }

    [Test]
    public void Deobfuscate_Float_ReturnsOriginalValue()
    {
        float original = 3.14f;

        float obfuscated = _encryptor.Obfuscate(original);
        float deobfuscated = _encryptor.Deobfuscate(obfuscated);

        Assert.That(deobfuscated, Is.EqualTo(original));
    }

    [Test]
    public void Deobfuscate_Long_ReturnsOriginalValue()
    {
        long original = 9876543210L;

        long obfuscated = _encryptor.Obfuscate(original);
        long deobfuscated = _encryptor.Deobfuscate(obfuscated);

        Assert.That(deobfuscated, Is.EqualTo(original));
    }

    #endregion

    #region GenerateHoneypot 测试

    [Test]
    public void GenerateHoneypot_ReturnsNonEmptyArray()
    {
        byte[] honeypot = _encryptor.GenerateHoneypot();

        Assert.That(honeypot, Is.Not.Null);
        Assert.That(honeypot.Length, Is.EqualTo(16));
    }

    [Test]
    public void GenerateHoneypot_ContainsExpectedPattern()
    {
        byte[] honeypot = _encryptor.GenerateHoneypot();

        int firstInt = BitConverter.ToInt32(honeypot, 0);

        Assert.That(firstInt, Is.EqualTo(99999));
    }

    #endregion
}
