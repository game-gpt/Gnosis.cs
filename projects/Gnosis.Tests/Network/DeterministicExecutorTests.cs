using Gnosis.Network;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Tests.Network
{
    public class DeterministicExecutorTests : GnosisTester
    {
        private DeterministicExecutor _executor = null!;

        public override void Setup()
        {
            base.Setup();
            _executor = new DeterministicExecutor(seed: 42);
        }

        [Test]
        public void CurrentSeed_返回初始种子()
        {
            Assert.That(_executor.CurrentSeed, Is.EqualTo(42));
        }

        [Test]
        public void RandomInt_相同种子产生相同序列()
        {
            var executor1 = new DeterministicExecutor(seed: 100);
            var executor2 = new DeterministicExecutor(seed: 100);

            var v1 = executor1.RandomInt(0, 1000);
            var v2 = executor2.RandomInt(0, 1000);

            Assert.That(v1, Is.EqualTo(v2));
        }

        [Test]
        public void RandomFloat_返回0到1之间的值()
        {
            var value = _executor.RandomFloat();

            Assert.That(value, Is.GreaterThanOrEqualTo(0f));
            Assert.That(value, Is.LessThan(1f));
        }

        [Test]
        public void RandomInt_返回指定范围内的值()
        {
            for (int i = 0; i < 100; i++)
            {
                var value = _executor.RandomInt(10, 20);
                Assert.That(value, Is.GreaterThanOrEqualTo(10));
                Assert.That(value, Is.LessThan(20));
            }
        }

        [Test]
        public void PushPopRandomState_恢复随机状态()
        {
            _executor.PushRandomState();
            var v1 = _executor.RandomInt(0, 10000);

            _executor.PopRandomState();
            var v2 = _executor.RandomInt(0, 10000);

            Assert.That(v1, Is.EqualTo(v2));
        }

        [Test]
        public void Execute_未注册函数抛出InvalidOperationException()
        {
            AssertThrows<InvalidOperationException>(() => _executor.Execute("nonexistent", 0, Array.Empty<byte>()));
        }

        [Test]
        public void Execute_注册函数后可执行()
        {
            _executor.RegisterFunction("test", new TestDeterministicFunction());

            var result = _executor.Execute("test", 1, [42]);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void LoggingEnabled_记录执行日志()
        {
            _executor.RegisterFunction("test", new TestDeterministicFunction());
            _executor.LoggingEnabled = true;

            _executor.Execute("test", 1, []);
            _executor.Execute("test", 2, []);

            Assert.That(_executor.LogCount, Is.EqualTo(2));
        }

        [Test]
        public void ClearLog_清除执行日志()
        {
            _executor.RegisterFunction("test", new TestDeterministicFunction());
            _executor.LoggingEnabled = true;
            _executor.Execute("test", 1, []);

            _executor.ClearLog();

            Assert.That(_executor.LogCount, Is.EqualTo(0));
        }

        [Test]
        public void ResetRandom_重置随机数生成器()
        {
            var v1 = _executor.RandomInt(0, 10000);
            _executor.ResetRandom(42);
            var v2 = _executor.RandomInt(0, 10000);

            Assert.That(v1, Is.EqualTo(v2));
        }

        private class TestDeterministicFunction : IDeterministicFunction
        {
            public byte[] Execute(int frame, byte[] input, DeterministicRandom random)
            {
                return [(byte)frame];
            }

            public int GetStateHash()
            {
                return 0;
            }
        }
    }
}
