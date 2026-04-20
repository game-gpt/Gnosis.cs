namespace Gnosis.Rendering.Interfaces;

public interface IMaterial
{
    ulong ShaderHandle { get; }
    
    T GetParameter<T>(string name) where T : struct;
    void SetParameter<T>(string name, T value) where T : struct;
}
