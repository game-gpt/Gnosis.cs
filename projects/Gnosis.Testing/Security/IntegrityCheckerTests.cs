using Gnosis.Security;
using NUnit.Framework;

namespace Gnosis.Security.Tests;

/// <summary>
/// 完整性检查器单元测试
/// </summary>
[TestFixture]
public class IntegrityCheckerTests
{
    #region 测试数据

    private static readonly byte[] OriginalBytes = [0x01, 0x02, 0x03, 0x04, 0x05];

    #endregion

    #region 测试方法

    [Test]
    public void Check_WithUnmodifiedBytes_ReturnsTrue()
    {
        var checker = new IntegrityChecker("test", OriginalBytes, () => OriginalBytes);

        var result = checker.Check();

        Assert.That(result, Is.True);
    }

    [Test]
    public void Check_WithModifiedBytes_ReturnsFalse()
    {
        var modifiedBytes = new byte[] { 0xFF, 0xFE, 0xFD, 0xFC, 0xFB };
        var checker = new IntegrityChecker("test", OriginalBytes, () => modifiedBytes);

        var result = checker.Check();

        Assert.That(result, Is.False);
    }

    [Test]
    public void Check_UpdatesLastCheckTime()
    {
        var checker = new IntegrityChecker("test", OriginalBytes, () => OriginalBytes);
        var before = checker.LastCheckTime;

        checker.Check();

        var after = checker.LastCheckTime;
        Assert.That(after, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void Name_ReturnsConstructorValue()
    {
        const string expectedName = "my_checker";
        var checker = new IntegrityChecker(expectedName, OriginalBytes, () => OriginalBytes);

        Assert.That(checker.Name, Is.EqualTo(expectedName));
    }

    [Test]
    public void Check_MultipleChecksSameData_ReturnsTrue()
    {
        var checker = new IntegrityChecker("test", OriginalBytes, () => OriginalBytes);

        var first = checker.Check();
        var second = checker.Check();

        Assert.That(first, Is.True);
        Assert.That(second, Is.True);
    }

    #endregion
}
