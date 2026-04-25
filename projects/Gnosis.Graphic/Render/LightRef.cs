using Gnosis.ECS.Component;
using Gnosis.Graphic.Light;

namespace Gnosis.Graphic.Render;

public struct LightRef : IComponent
{
    public ILight? Light { get; set; }

    public bool Enabled { get; set; }
}
