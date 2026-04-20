namespace GnosisEngine.RHI;

public interface IDevice
{
    IResource CreateBuffer(ulong size);
    IResource CreateTexture(uint width, uint height, Enums.ResourceFormat format);
    IResource CreateShader(byte[] spirvBytecode);
    IPipelineState CreatePipelineState(PipelineStateDesc desc);
    ICommandTable CreateCommandTable();

    void Submit(ICommandTable commandTable);
    void WaitIdle();
}
