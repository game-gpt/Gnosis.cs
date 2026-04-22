using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader;

/// <summary>
/// Shader 编译目标枚举（已迁移至 Gnosis.IR.Shader）
/// </summary>
public enum ShaderTarget
{
    Spirv,
    Glsl,
    Hlsl,
    Msl,
    Wgsl,
    Dxil,
    Neural,
    Diffusion
}
