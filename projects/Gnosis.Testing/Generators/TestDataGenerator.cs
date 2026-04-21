using Gnosis.Core;

namespace Gnosis.Testing.Generators
{
    public static class TestDataGenerator
    {
        private static readonly Random _random = new();

        public static int RandomInt(int min = 0, int max = 1000)
        {
            return _random.Next(min, max);
        }

        public static float RandomFloat(float min = 0.0f, float max = 100.0f)
        {
            return (float)(_random.NextDouble() * (max - min) + min);
        }

        public static string RandomString(int length = 10)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        public static Position RandomPosition()
        {
            return new Position(
                RandomFloat(-100.0f, 100.0f),
                RandomFloat(-100.0f, 100.0f),
                RandomFloat(-100.0f, 100.0f)
            );
        }

        public static List<Position> RandomPositions(int count)
        {
            var positions = new List<Position>();
            for (int i = 0; i < count; i++)
            {
                positions.Add(RandomPosition());
            }
            return positions;
        }

        public static EntityId RandomEntityId()
        {
            return EntityId.New();
        }

        public static PlayerId RandomPlayerId()
        {
            return PlayerId.New();
        }

        public static Timestamp RandomTimestamp()
        {
            return Timestamp.FromUnixTimeMilliseconds(
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + RandomInt(-10000, 10000));
        }

        public static bool RandomBool()
        {
            return _random.Next(2) == 0;
        }

        public static T RandomEnum<T>() where T : Enum
        {
            var values = Enum.GetValues(typeof(T));
            return (T)values.GetValue(_random.Next(values.Length))!;
        }

        public static List<T> RandomList<T>(Func<T> generator, int minCount = 1, int maxCount = 10)
        {
            var count = RandomInt(minCount, maxCount);
            var list = new List<T>();
            for (int i = 0; i < count; i++)
            {
                list.Add(generator());
            }
            return list;
        }

        public static Dictionary<TKey, TValue> RandomDictionary<TKey, TValue>(
            Func<TKey> keyGenerator,
            Func<TValue> valueGenerator,
            int minCount = 1,
            int maxCount = 10) where TKey : notnull
        {
            var count = RandomInt(minCount, maxCount);
            var dict = new Dictionary<TKey, TValue>();
            for (int i = 0; i < count; i++)
            {
                dict[keyGenerator()] = valueGenerator();
            }
            return dict;
        }
    }
}
