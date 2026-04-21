using Gnosis.Security;
using NUnit.Framework;

namespace Gnosis.Security.Tests;

/// <summary>
/// 调试器检测器单元测试，验证调试器检测功能的正确性
/// </summary>
[TestFixture]
public class DebugDetectorTests
{
    [Test]
    public void IsDebuggerPresent_ReturnsBool()
    {
        Assert.DoesNotThrow(() =>
        {
            var result = DebugDetector.IsDebuggerPresent();
            Assert.That(result, Is.InstanceOf<bool>());
        });
    }

    [Test]
    public void IsDebuggerPresent_WhenNotDebugging_ReturnsFalse()
    {
        var result = DebugDetector.IsDebuggerPresent();

        Assert.That(result, Is.False);
    }
}
