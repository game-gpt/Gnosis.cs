using Gnosis.AI.Navigation;
using Gnosis.ECS.Core;

namespace Gnosis.AI;

public struct NavAgentComponent : IComponent
{
    public IPath? CurrentPath { get; set; }
    public float Speed { get; set; }
    public float StoppingDistance { get; set; }
    public bool HasPath { get; set; }
}
