using Gnosis.Core.Math;

namespace Gnosis.Graphic.RHI;

public interface IView
{
    Matrix4x4 ViewMatrix { get; }
    Matrix4x4 ProjectionMatrix { get; }
    ulong RenderTarget { get; }
    RenderPathFlag RenderPathFlags { get; }
}
