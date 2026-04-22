namespace Gnosis.Graphic.RHI;

public interface IView
{
    float[] ViewMatrix { get; }
    float[] ProjectionMatrix { get; }
    ulong RenderTarget { get; }
    RenderPathFlag RenderPathFlags { get; }
}
