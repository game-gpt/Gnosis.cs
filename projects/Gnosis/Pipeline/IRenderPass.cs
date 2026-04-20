using GnosisEngine.Pipeline.ValueObjects;

namespace GnosisEngine.Pipeline;

public interface IRenderPass
{
    string Name { get; }
    bool Enabled { get; set; }

    void Execute(RenderContext context, RHI.ICommandTable commandTable);
}
