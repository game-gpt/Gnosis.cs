using Gnosis.ECS.Component;
using Gnosis.Navigation.Path;

namespace Gnosis.AI.State;

public struct NavAgentComponent : IComponent
{
    public IPath? CurrentPath { get; set; }
    public float Speed { get; set; }
    public float StoppingDistance { get; set; }
    public bool HasPath { get; set; }
}
