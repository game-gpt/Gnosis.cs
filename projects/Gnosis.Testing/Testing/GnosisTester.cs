using NUnit.Framework;

namespace Gnosis.Testing
{
    public abstract class GnosisTester
    {
        protected Dictionary<string, object>? TestContext { get; private set; }

        [SetUp]
        public virtual void Setup()
        {
            TestContext = new Dictionary<string, object>();
        }

        [TearDown]
        public virtual void Teardown()
        {
            TestContext?.Clear();
            TestContext = null;
        }

        protected void SetContext<T>(string key, T value)
        {
            if (TestContext is not null)
            {
                TestContext[key] = value!;
            }
        }

        protected T? GetContext<T>(string key)
        {
            if (TestContext is not null && TestContext.TryGetValue(key, out var value))
            {
                return (T)value;
            }
            return default;
        }

        protected void AssertCollectionsEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
        {
            var expectedList = expected.ToList();
            var actualList = actual.ToList();

            Assert.That(actualList.Count, Is.EqualTo(expectedList.Count),
                $"集合数量不匹配，期望 {expectedList.Count}，实际 {actualList.Count}");

            for (var i = 0; i < expectedList.Count; i++)
            {
                Assert.That(actualList[i], Is.EqualTo(expectedList[i]),
                    $"集合索引 {i} 处的元素不匹配");
            }
        }

        protected void AssertThrows<T>(Action action, string? expectedMessage = null) where T : Exception
        {
            var exception = Assert.Throws<T>(() => action());
            if (expectedMessage is not null && exception is not null)
            {
                Assert.That(exception.Message, Does.Contain(expectedMessage));
            }
        }

        protected async Task AssertThrowsAsync<T>(Func<Task> action, string? expectedMessage = null) where T : Exception
        {
            try
            {
                await action();
                Assert.Fail($"期望抛出 {typeof(T).Name}，但未抛出任何异常");
            }
            catch (T ex)
            {
                if (expectedMessage is not null)
                {
                    Assert.That(ex.Message, Does.Contain(expectedMessage));
                }
            }
        }
    }
}
