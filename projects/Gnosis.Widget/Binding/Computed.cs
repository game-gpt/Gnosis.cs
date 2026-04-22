namespace Gnosis.Widget.Binding;

public sealed class Computed<T> : IObservable<T>, IDisposable
{
    private readonly Func<T> _compute;
    private readonly List<IObservable> _dependencies;
    private T _value;
    private bool _disposed;

    public T Value
    {
        get
        {
            if (_disposed)
            {
                return _value;
            }

            return _value;
        }
    }

    public event Action? Changed;

    public Computed(Func<T> compute, params IObservable[] dependencies)
    {
        _compute = compute;
        _dependencies = [.. dependencies];
        _value = compute();

        foreach (var dep in _dependencies)
        {
            dep.Changed += OnDependencyChanged;
        }
    }

    public void Recompute()
    {
        var newValue = _compute();

        if (EqualityComparer<T>.Default.Equals(_value, newValue))
        {
            return;
        }

        _value = newValue;
        Changed?.Invoke();
    }

    private void OnDependencyChanged()
    {
        Recompute();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var dep in _dependencies)
        {
            dep.Changed -= OnDependencyChanged;
        }
    }
}
