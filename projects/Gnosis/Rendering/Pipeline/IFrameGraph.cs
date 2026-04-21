using Gnosis.Rendering.RHI;

namespace Gnosis.Rendering.Pipeline;

public interface IFrameGraph
{
    IRenderPass AddPass(string name, Action<RenderContext, ICommandTable> execute);
    IResource ImportResource(string name, IResource resource);
    ResourceHandle CreateResource(string name, ResourceDesc desc);

    void Compile();
    void Execute(RenderContext context);
}

public readonly record struct ResourceHandle(ulong Id);
