namespace Gnosis.Renderer;

public interface ICommandTable
{
    void Begin();
    void End();
    
    void SetPipelineState(IPipelineState pipelineState);
    void SetVertexBuffer(IResource buffer, ulong offset = 0);
    void SetIndexBuffer(IResource buffer, ulong offset = 0);
    void SetTexture(IResource texture, uint slot);
    
    void Draw(uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0);
    void DrawIndexed(uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0);
    void Dispatch(uint groupCountX, uint groupCountY, uint groupCountZ);
}
