using Gnosis.Input.Device;

namespace Gnosis.Input.Binding;

public sealed class InputBinding : IInputBinding
{
    #region 字段

    private readonly List<IInputBinding> _compositeBindings = new();

    #endregion

    #region 属性

    public string Name { get; }

    public string Path { get; }

    public InputDeviceType DeviceType { get; }

    public bool IsComposite { get; }

    public IReadOnlyList<IInputBinding> CompositeBindings => _compositeBindings;

    #endregion

    #region 构造函数

    public InputBinding(string name, string path, InputDeviceType deviceType, bool isComposite = false)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Path = path ?? throw new ArgumentNullException(nameof(path));
        DeviceType = deviceType;
        IsComposite = isComposite;
    }

    #endregion

    #region 公开方法

    public void AddCompositeBinding(IInputBinding binding)
    {
        if (!IsComposite)
        {
            throw new InvalidOperationException("非组合绑定不能添加子绑定");
        }

        _compositeBindings.Add(binding ?? throw new ArgumentNullException(nameof(binding)));
    }

    #endregion
}
