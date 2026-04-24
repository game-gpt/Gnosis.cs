namespace Gnosis.Core.Event;

public interface IRepository<T> : LightDB.Core.IRepository<T> where T : class
{
}
