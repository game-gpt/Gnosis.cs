using Gnosis.Core;

namespace Gnosis.Input;

public struct InputComponent : IComponent
{
    public string ActionMapName { get; set; }
    public bool IsEnabled { get; set; }
}
