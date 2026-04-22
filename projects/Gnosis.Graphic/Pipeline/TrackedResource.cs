namespace Gnosis.Graphic.Pipeline;

public sealed class TrackedResource
{
    public ResourceHandle Handle { get; }
    public string Name { get; }
    public ResourceDesc Desc { get; }
    public bool IsImported { get; }
    public RHI.IResource? PhysicalResource { get; set; }

    internal int FirstUsePass { get; set; } = int.MaxValue;
    internal int LastUsePass { get; set; } = int.MinValue;
    internal bool NeedsAllocation { get; set; }

    public TrackedResource(ResourceHandle handle, string name, ResourceDesc desc, bool isImported = false)
    {
        Handle = handle;
        Name = name;
        Desc = desc;
        IsImported = isImported;
    }
}
