namespace Gnosis.ECS.Interface;

public interface IComponentStorage
{
    IComponentPool GetPool<T>() where T : struct;
    IArchetype GetArchetypeStorage(params Type[] componentTypes);
}
