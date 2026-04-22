using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Pipeline;

public sealed class FrameGraph : IFrameGraph
{
    private readonly IDevice _device;
    private readonly List<RenderPass> _passes = [];
    private readonly Dictionary<ulong, TrackedResource> _resources = [];
    private readonly List<TrackedResource> _transientResources = [];
    private ulong _nextResourceId;

    private bool _isCompiled;

    public FrameGraph(IDevice device)
    {
        _device = device;
    }

    public IRenderPass AddPass(string name, Action<RenderContext, ICommandTable> execute)
    {
        var pass = new RenderPass(name, execute);
        _passes.Add(pass);
        _isCompiled = false;
        return pass;
    }

    public IResource ImportResource(string name, IResource resource)
    {
        var handle = new ResourceHandle(++_nextResourceId);
        var desc = new ResourceDesc
        {
            Width = 0,
            Height = 0,
            Format = ResourceFormat.Unknown,
            Usage = ResourceUsage.None
        };
        var tracked = new TrackedResource(handle, name, desc, isImported: true)
        {
            PhysicalResource = resource
        };
        _resources[handle.Id] = tracked;
        _isCompiled = false;
        return resource;
    }

    public ResourceHandle CreateResource(string name, ResourceDesc desc)
    {
        var handle = new ResourceHandle(++_nextResourceId);
        var tracked = new TrackedResource(handle, name, desc);
        _resources[handle.Id] = tracked;
        _isCompiled = false;
        return handle;
    }

    public void Compile()
    {
        if (_isCompiled)
        {
            return;
        }

        CalculateResourceLifetimes();
        AllocateTransientResources();
        _isCompiled = true;
    }

    public void Execute(RenderContext context)
    {
        if (!_isCompiled)
        {
            Compile();
        }

        foreach (var pass in _passes)
        {
            if (!pass.Enabled)
            {
                continue;
            }

            var commandTable = _device.CreateCommandTable();
            commandTable.Begin();
            pass.Execute(context, commandTable);
            commandTable.End();
            _device.Submit(commandTable);
            commandTable.Dispose();
        }
    }

    public void Reset()
    {
        foreach (var resource in _transientResources)
        {
            resource.PhysicalResource?.Dispose();
        }

        _passes.Clear();
        _resources.Clear();
        _transientResources.Clear();
        _nextResourceId = 0;
        _isCompiled = false;
    }

    public TrackedResource? GetResource(ResourceHandle handle)
    {
        return _resources.GetValueOrDefault(handle.Id);
    }

    private void CalculateResourceLifetimes()
    {
        for (var passIndex = 0; passIndex < _passes.Count; passIndex++)
        {
            var pass = _passes[passIndex];
            UpdateResourceLifetime(pass.Reads, passIndex);
            UpdateResourceLifetime(pass.Writes, passIndex);
        }
    }

    private void UpdateResourceLifetime(HashSet<ResourceHandle> handles, int passIndex)
    {
        foreach (var handle in handles)
        {
            if (!_resources.TryGetValue(handle.Id, out var resource))
            {
                continue;
            }

            if (passIndex < resource.FirstUsePass)
            {
                resource.FirstUsePass = passIndex;
            }

            if (passIndex > resource.LastUsePass)
            {
                resource.LastUsePass = passIndex;
            }
        }
    }

    private void AllocateTransientResources()
    {
        foreach (var resource in _resources.Values)
        {
            if (resource.IsImported || resource.PhysicalResource != null)
            {
                continue;
            }

            if (resource.FirstUsePass == int.MaxValue)
            {
                continue;
            }

            var desc = new TextureDesc
            {
                Dimension = TextureDimension.Texture2D,
                Width = resource.Desc.Width,
                Height = resource.Desc.Height,
                Format = resource.Desc.Format,
                Usage = ConvertResourceUsage(resource.Desc.Usage),
                MipLevels = 1,
                ArrayLayers = 1,
                Depth = 1,
                SampleCount = 1
            };

            resource.PhysicalResource = _device.CreateTexture(desc);
            resource.NeedsAllocation = false;
            _transientResources.Add(resource);
        }
    }

    private static TextureUsage ConvertResourceUsage(ResourceUsage usage)
    {
        var result = TextureUsage.None;

        if (usage.HasFlag(ResourceUsage.RenderTarget))
        {
            result |= TextureUsage.RenderTarget;
        }

        if (usage.HasFlag(ResourceUsage.DepthStencil))
        {
            result |= TextureUsage.DepthStencil;
        }

        if (usage.HasFlag(ResourceUsage.ShaderResource))
        {
            result |= TextureUsage.ShaderResource;
        }

        if (usage.HasFlag(ResourceUsage.UnorderedAccess))
        {
            result |= TextureUsage.UnorderedAccess;
        }

        if (usage.HasFlag(ResourceUsage.TransferSrc))
        {
            result |= TextureUsage.TransferSrc;
        }

        if (usage.HasFlag(ResourceUsage.TransferDst))
        {
            result |= TextureUsage.TransferDst;
        }

        return result;
    }
}
