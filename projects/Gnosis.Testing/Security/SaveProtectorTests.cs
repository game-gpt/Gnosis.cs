using Gnosis.Security;
using NUnit.Framework;

namespace Gnosis.Security.Tests;

/// <summary>
/// 存档保护器单元测试，验证 AES-256-GCM 加密解密及硬件指纹绑定功能
/// </summary>
[TestFixture]
public class SaveProtectorTests
{
    #region 字段

    private SaveProtector _protector = null!;

    #endregion

    #region Setup / Teardown

    [SetUp]
    public void Setup()
    {
        _protector = new SaveProtector();
    }

    #endregion

    #region EncryptSave 测试

    [Test]
    public void EncryptSave_WithValidData_ReturnsNonEmptyResult()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var hardwareId = "hw-test-001";

        var result = _protector.EncryptSave(data, hardwareId);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Length, Is.GreaterThan(data.Length));
    }

    [Test]
    public void EncryptSave_NullData_ThrowsSecurityException()
    {
        var hardwareId = "hw-test-001";

        Assert.Throws<SecurityException>(() => _protector.EncryptSave(null!, hardwareId));
    }

    [Test]
    public void EncryptSave_EmptyData_ThrowsSecurityException()
    {
        var data = Array.Empty<byte>();
        var hardwareId = "hw-test-001";

        Assert.Throws<SecurityException>(() => _protector.EncryptSave(data, hardwareId));
    }

    [Test]
    public void EncryptSave_NullHardwareId_ThrowsSecurityException()
    {
        var data = new byte[] { 1, 2, 3 };

        Assert.Throws<SecurityException>(() => _protector.EncryptSave(data, null!));
    }

    [Test]
    public void EncryptSave_LargeData_Roundtrip()
    {
        var data = new byte[10000];
        var random = new Random(42);
        random.NextBytes(data);
        var hardwareId = "hw-large-data";

        var encrypted = _protector.EncryptSave(data, hardwareId);
        var decrypted = _protector.DecryptSave(encrypted, hardwareId);

        Assert.That(decrypted, Is.EqualTo(data));
    }

    #endregion

    #region DecryptSave 测试

    [Test]
    public void DecryptSave_WithSameHardwareId_ReturnsOriginalData()
    {
        var data = new byte[] { 10, 20, 30, 40, 50 };
        var hardwareId = "hw-roundtrip";

        var encrypted = _protector.EncryptSave(data, hardwareId);
        var decrypted = _protector.DecryptSave(encrypted, hardwareId);

        Assert.That(decrypted, Is.EqualTo(data));
    }

    [Test]
    public void DecryptSave_WithDifferentHardwareId_ThrowsSecurityException()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        var hardwareId1 = "hw-original";
        var hardwareId2 = "hw-different";

        var encrypted = _protector.EncryptSave(data, hardwareId1);

        Assert.Throws<SecurityException>(() => _protector.DecryptSave(encrypted, hardwareId2));
    }

    [Test]
    public void DecryptSave_NullData_ThrowsSecurityException()
    {
        var hardwareId = "hw-test-001";

        Assert.Throws<SecurityException>(() => _protector.DecryptSave(null!, hardwareId));
    }

    [Test]
    public void DecryptSave_TooShortData_ThrowsSecurityException()
    {
        var shortData = new byte[27];
        var hardwareId = "hw-test-001";

        Assert.Throws<SecurityException>(() => _protector.DecryptSave(shortData, hardwareId));
    }

    #endregion

    #region GetHardwareFingerprint 测试

    [Test]
    public void GetHardwareFingerprint_ReturnsNonEmptyString()
    {
        var fingerprint = SaveProtector.GetHardwareFingerprint();

        Assert.That(fingerprint, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void GetHardwareFingerprint_ReturnsConsistentResult()
    {
        var fingerprint1 = SaveProtector.GetHardwareFingerprint();
        var fingerprint2 = SaveProtector.GetHardwareFingerprint();

        Assert.That(fingerprint1, Is.EqualTo(fingerprint2));
    }

    [Test]
    public void GetHardwareFingerprint_ReturnsLowerCaseHexString()
    {
        var fingerprint = SaveProtector.GetHardwareFingerprint();

        Assert.That(fingerprint, Does.Match("^[0-9a-f]+$"));
    }

    #endregion
}
