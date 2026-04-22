namespace Gnosis.Widget.Binding;

public abstract class ViewModelBase : IDisposable
{
    private readonly List<IObservable> _observables = new();
    private readonly List<DataBinding> _bindings = new();
    private bool _disposed;

    public event Action? PropertyChanged;

    protected Observable<T> Observable<T>(T initialValue)
    {
        var observable = new Observable<T>(initialValue);
        _observables.Add(observable);
        observable.Changed += () => PropertyChanged?.Invoke();
        return observable;
    }

    protected Computed<T> Computed<T>(Func<T> compute, params IObservable[] dependencies)
    {
        var computed = new Computed<T>(compute, dependencies);
        _observables.Add(computed);
        computed.Changed += () => PropertyChanged?.Invoke();
        return computed;
    }

    internal void AddBinding(DataBinding binding)
    {
        _bindings.Add(binding);
    }

    internal void RemoveBinding(DataBinding binding)
    {
        _bindings.Remove(binding);
    }

    public IReadOnlyList<DataBinding> Bindings => _bindings;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var binding in _bindings)
        {
            binding.Dispose();
        }

        _bindings.Clear();

        foreach (var observable in _observables)
        {
            if (observable is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        _observables.Clear();
    }
}
