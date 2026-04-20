namespace Gnosis.ECS.Interfaces;

public interface IComponentStorage
{
    IComponentPool GetPool<T>() where T : struct;
    IArchetype GetArchetypeStorage(params Type[] componentTypes);
}
