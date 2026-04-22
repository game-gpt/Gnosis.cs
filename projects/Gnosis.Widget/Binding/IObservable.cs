namespace Gnosis.Widget.Binding;

public interface IObservable
{
    event Action? Changed;
}

public interface IObservable<T> : IObservable
{
    T Value { get; }
}
