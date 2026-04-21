using Gnosis.ECS.Core;

namespace Gnosis.ECS.Interface;

public interface IQuery
{
    IQuery All<T>() where T : struct;
    IQuery Any<T>() where T : struct;
    IQuery None<T>() where T : struct;
    
    IEnumerable<EntityId> Build();
}
