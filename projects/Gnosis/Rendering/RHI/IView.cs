namespace Gnosis.Rendering.RHI;

public interface IView
{
    float[] ViewMatrix { get; }
    float[] ProjectionMatrix { get; }
    ulong RenderTarget { get; }
    RenderPathFlag RenderPathFlags { get; }
}
