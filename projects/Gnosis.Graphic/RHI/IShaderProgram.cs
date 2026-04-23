namespace Gnosis.Graphic.RHI;

/// <summary>
///     着色器程序接口，封装编译后的着色器资源
/// </summary>
public interface IShaderProgram : IDisposable
{
    /// <summary>
    ///     着色器名称
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     着色器阶段标志
    /// </summary>
    ShaderStageFlags Stages { get; }
}

/// <summary>
///     着色器阶段标志
/// </summary>
[Flags]
public enum ShaderStageFlags : uint
{
    Vertex = 1 << 0,
    Fragment = 1 << 1,
    Geometry = 1 << 2,
    Compute = 1 << 3,
    TessellationControl = 1 << 4,
    TessellationEvaluation = 1 << 5
}
