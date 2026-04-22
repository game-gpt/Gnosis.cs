using System.Reflection;
using Gnosis.Widget.Element;

namespace Gnosis.Widget.Binding;

public sealed class DataBinding : IDisposable
{
    private readonly WidgetElement _target;
    private readonly string _targetProperty;
    private readonly BindingMode _mode;
    private readonly IObservable? _sourceObservable;
    private readonly string? _sourcePath;
    private readonly PropertyInfo? _targetPropertyInfo;
    private bool _disposed;
    private bool _updating;

    public WidgetElement Target => _target;
    public string TargetProperty => _targetProperty;
    public BindingMode Mode => _mode;

    public DataBinding(WidgetElement target, string targetProperty, IObservable source, BindingMode mode = BindingMode.OneWay)
    {
        _target = target;
        _targetProperty = targetProperty;
        _mode = mode;
        _sourceObservable = source;
        _targetPropertyInfo = target.GetType().GetProperty(targetProperty);

        _sourceObservable.Changed += OnSourceChanged;

        ApplySourceToTarget();
    }

    public DataBinding(WidgetElement target, string targetProperty, string sourcePath, BindingMode mode = BindingMode.OneWay)
    {
        _target = target;
        _targetProperty = targetProperty;
        _mode = mode;
        _sourcePath = sourcePath;
        _targetPropertyInfo = target.GetType().GetProperty(targetProperty);
    }

    public void Update()
    {
        if (_disposed)
        {
            return;
        }

        if (_sourceObservable != null)
        {
            ApplySourceToTarget();
        }
    }

    private void OnSourceChanged()
    {
        if (_disposed || _updating)
        {
            return;
        }

        ApplySourceToTarget();
    }

    private void ApplySourceToTarget()
    {
        if (_targetPropertyInfo == null || _sourceObservable == null)
        {
            return;
        }

        _updating = true;

        try
        {
            var sourceValue = GetObservableValue(_sourceObservable);

            if (sourceValue != null)
            {
                var targetType = _targetPropertyInfo.PropertyType;

                if (targetType.IsAssignableFrom(sourceValue.GetType()))
                {
                    _targetPropertyInfo.SetValue(_target, sourceValue);
                }
                else
                {
                    var converted = Convert.ChangeType(sourceValue, targetType);
                    _targetPropertyInfo.SetValue(_target, converted);
                }

                _target.InvalidateArrange();
            }
        }
        finally
        {
            _updating = false;
        }
    }

    private static object? GetObservableValue(IObservable observable)
    {
        var type = observable.GetType();

        if (type.IsGenericType)
        {
            var valueProp = type.GetProperty("Value");
            return valueProp?.GetValue(observable);
        }

        return null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_sourceObservable != null)
        {
            _sourceObservable.Changed -= OnSourceChanged;
        }
    }
}
