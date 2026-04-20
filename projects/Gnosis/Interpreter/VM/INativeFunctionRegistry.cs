namespace Gnosis.Interpreter.VM;

public interface INativeFunctionRegistry
{
    void Register(INativeFunction function);
    void Unregister(int id);
    void Unregister(string name);
    INativeFunction? Get(int id);
    INativeFunction? Get(string name);
    bool Contains(int id);
    bool Contains(string name);
    IReadOnlyList<INativeFunction> GetAll();
    void Clear();
}
