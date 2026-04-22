namespace Gnosis.Widget.Binding;

public sealed class Observable<T> : IObservable<T>
{
    private T _value;

    public T Value
    {
        get => _value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value))
            {
                return;
            }

            _value = value;
            Changed?.Invoke();
        }
    }

    public event Action? Changed;

    public Observable(T value)
    {
        _value = value;
    }

    public static implicit operator T(Observable<T> observable) => observable.Value;
}
