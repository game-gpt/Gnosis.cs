namespace Gnosis.Core.Event;

public interface IRepository<T> where T : class
{
    T? FindById(string id);
    IEnumerable<T> FindAll();
    void Add(T entity);
    void Remove(T entity);
    void Update(T entity);
}
