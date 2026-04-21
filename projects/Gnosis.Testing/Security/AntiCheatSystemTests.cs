using Gnosis.Infrastructure;
using Gnosis.Security;
using NUnit.Framework;

namespace Gnosis.Security.Tests;

/// <summary>
/// 反作弊系统单元测试
/// </summary>
[TestFixture]
public class AntiCheatSystemTests
{
    #region 字段

    private AntiCheatSystem _system = null!;

    #endregion

    #region Setup

    [SetUp]
    public void Setup()
    {
        _system = new AntiCheatSystem();
    }

    #endregion

    #region 初始化与关闭测试

    [Test]
    public void Initialize_SetsUpSystem()
    {
        Assert.DoesNotThrow(() => _system.Initialize());
    }

    [Test]
    public void Shutdown_ClearsSystem()
    {
        _system.Initialize();

        Assert.DoesNotThrow(() => _system.Shutdown());
    }

    #endregion

    #region 注册检查器测试

    [Test]
    public void RegisterChecker_AddsChecker()
    {
        _system.Initialize();
        var checker = new MockIntegrityChecker("checker_a", true);
        _system.RegisterChecker(checker);

        var result = _system.PerformCheck("checker_a");

        Assert.That(result, Is.True);
    }

    [Test]
    public void RegisterChecker_DuplicateName_ThrowsSecurityException()
    {
        _system.Initialize();
        var checker1 = new MockIntegrityChecker("dup", true);
        var checker2 = new MockIntegrityChecker("dup", false);
        _system.RegisterChecker(checker1);

        Assert.Throws<SecurityException>(() => _system.RegisterChecker(checker2));
    }

    #endregion

    #region 执行检查测试

    [Test]
    public void PerformCheck_WithPassingChecker_ReturnsTrue()
    {
        _system.Initialize();
        _system.RegisterChecker(new MockIntegrityChecker("pass", true));

        var result = _system.PerformCheck("pass");

        Assert.That(result, Is.True);
    }

    [Test]
    public void PerformCheck_WithFailingChecker_ReturnsFalse()
    {
        _system.Initialize();
        _system.RegisterChecker(new MockIntegrityChecker("fail", false));

        var result = _system.PerformCheck("fail");

        Assert.That(result, Is.False);
    }

    [Test]
    public void PerformCheck_FailingChecker_SetsIsCompromised()
    {
        _system.Initialize();
        _system.RegisterChecker(new MockIntegrityChecker("fail", false));

        _system.PerformCheck("fail");

        Assert.That(_system.IsCompromised, Is.True);
    }

    [Test]
    public void PerformCheck_FailingChecker_FiresOnViolation()
    {
        _system.Initialize();
        _system.RegisterChecker(new MockIntegrityChecker("fail", false));

        ViolationEventArgs? capturedArgs = null;
        _system.OnViolation += (_, args) => capturedArgs = args;

        _system.PerformCheck("fail");

        Assert.That(capturedArgs, Is.Not.Null);
        Assert.That(capturedArgs!.Type, Is.EqualTo(ViolationType.IntegrityCheckFailed));
        Assert.That(capturedArgs.Response, Is.EqualTo(ViolationResponse.Log));
        Assert.That(capturedArgs.Details, Does.Contain("fail"));
    }

    [Test]
    public void PerformCheck_UnknownChecker_ThrowsSecurityException()
    {
        _system.Initialize();

        Assert.Throws<SecurityException>(() => _system.PerformCheck("nonexistent"));
    }

    #endregion

    #region IsCompromised 测试

    [Test]
    public void IsCompromised_StartsFalse()
    {
        Assert.That(_system.IsCompromised, Is.False);
    }

    [Test]
    public void IsCompromised_NeverResets()
    {
        _system.Initialize();
        _system.RegisterChecker(new MockIntegrityChecker("fail", false));
        _system.RegisterChecker(new MockIntegrityChecker("pass", true));

        _system.PerformCheck("fail");
        Assert.That(_system.IsCompromised, Is.True);

        _system.PerformCheck("pass");
        Assert.That(_system.IsCompromised, Is.True);
    }

    #endregion

    #region Mock 类

    private class MockIntegrityChecker : IIntegrityChecker
    {
        public string Name { get; }
        public Timestamp LastCheckTime { get; private set; }
        private readonly bool _checkResult;

        public MockIntegrityChecker(string name, bool checkResult)
        {
            Name = name;
            _checkResult = checkResult;
        }

        public bool Check()
        {
            LastCheckTime = Timestamp.Now;
            return _checkResult;
        }
    }

    #endregion
}
