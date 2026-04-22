namespace Gnosis.Core.Event;

public interface IRepository<T> : SolidDB.Core.IRepository<T> where T : class
{
}
