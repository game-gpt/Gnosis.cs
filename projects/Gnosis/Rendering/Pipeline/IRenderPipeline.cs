namespace Gnosis.Rendering.Pipeline;

public interface IRenderPipeline
{
    string Name { get; }
    IReadOnlyList<IRenderPass> Passes { get; }

    void Render(RenderContext context);
    void AddPass(IRenderPass pass);
    bool RemovePass(string passName);
}
