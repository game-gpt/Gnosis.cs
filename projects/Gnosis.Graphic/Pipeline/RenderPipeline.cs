namespace Gnosis.Graphic.Pipeline;

public sealed class RenderPipeline : IRenderPipeline
{
    private readonly List<IRenderPass> _passes = [];

    public string Name { get; }
    public IReadOnlyList<IRenderPass> Passes => _passes;

    public RenderPipeline(string name)
    {
        Name = name;
    }

    public void Render(RenderContext context)
    {
        foreach (var pass in _passes)
        {
            if (!pass.Enabled)
            {
                continue;
            }

            ExecutePass(pass, context);
        }
    }

    public void AddPass(IRenderPass pass)
    {
        _passes.Add(pass);
    }

    public bool RemovePass(string passName)
    {
        for (var i = 0; i < _passes.Count; i++)
        {
            if (_passes[i].Name != passName)
            {
                continue;
            }

            _passes.RemoveAt(i);
            return true;
        }

        return false;
    }

    private static void ExecutePass(IRenderPass pass, RenderContext context)
    {
        if (context.Device is null)
        {
            return;
        }

        var commandTable = context.Device.CreateCommandTable();
        commandTable.Begin();
        pass.Execute(context, commandTable);
        commandTable.End();
        context.Device.Submit(commandTable);
        commandTable.Dispose();
    }
}
