using System.Reflection;
using Gnosis.Security;
using NUnit.Framework;

namespace Gnosis.Security.Tests;

/// <summary>
/// EncryptedField 单元测试，验证加密字段的值存取、加密存储、原始值记录及加密状态判断功能
/// </summary>
[TestFixture]
public class EncryptedFieldTests
{
    #region Value 存取测试

    [Test]
    public void Value_SetAndGet_ReturnsSameValue()
    {
        var field = new EncryptedField<int>();

        field.Value = 100;

        Assert.That(field.Value, Is.EqualTo(100));
    }

    [Test]
    public void Value_StoresEncryptedInMemory()
    {
        var field = new EncryptedField<int>();
        int originalValue = 42;

        field.Value = originalValue;

        var encryptedValueField = typeof(EncryptedField<int>).GetField("_encryptedValue",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(encryptedValueField, Is.Not.Null);
        int storedValue = (int)encryptedValueField!.GetValue(field)!;

        Assert.That(storedValue, Is.Not.EqualTo(originalValue));
    }

    [Test]
    public void Value_FloatRoundtrip()
    {
        var field = new EncryptedField<float>();
        float original = 2.718f;

        field.Value = original;

        Assert.That(field.Value, Is.EqualTo(original));
    }

    [Test]
    public void Value_MultipleSetOperations()
    {
        var field = new EncryptedField<int>();

        field.Value = 10;
        Assert.That(field.Value, Is.EqualTo(10));

        field.Value = 20;
        Assert.That(field.Value, Is.EqualTo(20));

        field.Value = 30;
        Assert.That(field.Value, Is.EqualTo(30));
    }

    #endregion

    #region OriginalValue 测试

    [Test]
    public void OriginalValue_ReturnsFirstSetValue()
    {
        var field = new EncryptedField<int>();

        field.Value = 100;
        field.Value = 200;

        Assert.That(field.OriginalValue, Is.EqualTo(100));
    }

    #endregion

    #region IsEncrypted 测试

    [Test]
    public void IsEncrypted_AfterSettingValue_ReturnsTrue()
    {
        var field = new EncryptedField<int>();

        field.Value = 42;

        Assert.That(field.IsEncrypted, Is.True);
    }

    [Test]
    public void IsEncrypted_BeforeSettingValue_ReturnsFalse()
    {
        var field = new EncryptedField<int>();

        Assert.That(field.IsEncrypted, Is.False);
    }

    #endregion

    #region 构造函数测试

    [Test]
    public void DefaultConstructor_ValueReturnsDefault()
    {
        var field = new EncryptedField<int>();

        Assert.That(field.Value, Is.EqualTo(default(int)));
    }

    [Test]
    public void ConstructorWithInitialValue_SetsValueAndOriginal()
    {
        var field = new EncryptedField<int>(42);

        Assert.That(field.Value, Is.EqualTo(42));
        Assert.That(field.OriginalValue, Is.EqualTo(42));
    }

    #endregion
}
