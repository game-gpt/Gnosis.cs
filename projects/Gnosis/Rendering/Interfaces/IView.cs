namespace Gnosis.Rendering.Interfaces;

public interface IView
{
    float[] ViewMatrix { get; }
    float[] ProjectionMatrix { get; }
    ulong RenderTarget { get; }
    Enums.RenderPathFlag RenderPathFlags { get; }
}
