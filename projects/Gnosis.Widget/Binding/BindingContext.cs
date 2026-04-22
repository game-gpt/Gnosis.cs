using Gnosis.Widget.Element;

namespace Gnosis.Widget.Binding;

public sealed class BindingContext : IDisposable
{
    private readonly List<DataBinding> _bindings = new();
    private readonly Dictionary<WidgetElement, ViewModelBase> _viewModels = new();
    private bool _disposed;

    public DataBinding Bind(WidgetElement target, string targetProperty, IObservable source, BindingMode mode = BindingMode.OneWay)
    {
        var binding = new DataBinding(target, targetProperty, source, mode);
        _bindings.Add(binding);
        return binding;
    }

    public DataBinding Bind<T>(WidgetElement target, string targetProperty, Observable<T> source, BindingMode mode = BindingMode.OneWay)
    {
        return Bind(target, targetProperty, (IObservable)source, mode);
    }

    public void BindViewModel(WidgetElement target, ViewModelBase viewModel)
    {
        _viewModels[target] = viewModel;
    }

    public ViewModelBase? GetViewModel(WidgetElement target)
    {
        return _viewModels.GetValueOrDefault(target);
    }

    public void Unbind(WidgetElement target)
    {
        _bindings.RemoveAll(b =>
        {
            if (b.Target != target)
            {
                return false;
            }

            b.Dispose();
            return true;
        });

        if (_viewModels.Remove(target, out var vm))
        {
            vm.Dispose();
        }
    }

    public void UpdateAll()
    {
        foreach (var binding in _bindings)
        {
            binding.Update();
        }
    }

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

        foreach (var vm in _viewModels.Values)
        {
            vm.Dispose();
        }

        _viewModels.Clear();
    }
}
