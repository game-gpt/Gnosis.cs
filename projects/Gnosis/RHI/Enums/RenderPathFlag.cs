namespace GnosisEngine.RHI.Enums;

[Flags]
public enum RenderPathFlag
{
    None = 0,
    Raster = 1 << 0,
    NeuralBaked = 1 << 1,
    Diffusion = 1 << 2,
    Hybrid = 1 << 3
}
