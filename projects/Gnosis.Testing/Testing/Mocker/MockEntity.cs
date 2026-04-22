using Gnosis.ECS.Entity;

namespace Gnosis.Testing.Mocker;

public class MockEntity : IEntity
{
    public EntityId Id { get; }

    public MockEntity(EntityId id)
    {
        Id = id;
    }

    public MockEntity() : this(new EntityId(1, 1))
    {
    }

    public override bool Equals(object? obj)
    {
        if (obj is MockEntity other)
        {
            return Id.Equals(other.Id);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public override string ToString()
    {
        return $"Entity({Id})";
    }
}
