using Gnosis.Core;
using Gnosis.ECS;

namespace Gnosis.Testing.Mocks
{
    public static class MockFactory
    {
        public static MockWorld CreateWorld()
        {
            return new MockWorld();
        }

        public static MockEntity CreateEntity()
        {
            return new MockEntity();
        }

        public static EntityId CreateEntityInWorld(MockWorld world)
        {
            return world.CreateEntity();
        }

        public static List<EntityId> CreateEntities(MockWorld world, int count)
        {
            var entities = new List<EntityId>();
            for (var i = 0; i < count; i++)
            {
                entities.Add(world.CreateEntity());
            }
            return entities;
        }
    }
}
