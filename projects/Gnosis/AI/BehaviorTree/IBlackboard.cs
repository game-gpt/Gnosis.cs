namespace Gnosis.AI.BehaviorTree;

public interface IBlackboard
{
    void SetValue<T>(string key, T value);
    T? GetValue<T>(string key);
    bool HasKey(string key);
    void RemoveKey(string key);
    void Clear();
}
