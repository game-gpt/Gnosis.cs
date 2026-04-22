namespace Gnosis.Graphic.Pipeline;

public sealed class RenderPass : IRenderPass
{
    private readonly Action<RenderContext, RHI.ICommandTable> _execute;

    public string Name { get; }
    public bool Enabled { get; set; } = true;

    internal HashSet<ResourceHandle> Reads { get; } = [];
    internal HashSet<ResourceHandle> Writes { get; } = [];
    internal List<ResourceHandle> RenderTargets { get; } = [];
    internal ResourceHandle? DepthStencil { get; private set; }

    public RenderPass(string name, Action<RenderContext, RHI.ICommandTable> execute)
    {
        Name = name;
        _execute = execute;
    }

    public void Execute(RenderContext context, RHI.ICommandTable commandTable)
    {
        if (!Enabled)
        {
            return;
        }

        _execute(context, commandTable);
    }

    internal void AddRead(ResourceHandle handle)
    {
        Reads.Add(handle);
    }

    internal void AddWrite(ResourceHandle handle)
    {
        Writes.Add(handle);
    }

    internal void AddRenderTarget(ResourceHandle handle)
    {
        RenderTargets.Add(handle);
        Writes.Add(handle);
    }

    internal void SetDepthStencil(ResourceHandle handle)
    {
        DepthStencil = handle;
        Writes.Add(handle);
    }
}
