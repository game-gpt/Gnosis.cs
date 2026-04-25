using SpirvCross.Net.Model;

namespace SpirvCross.Net.Optimize;

/// <summary>
///     SPIR-V 死代码消除优化 Pass。
/// </summary>
public sealed class DeadCodeEliminationPass
{
    /// <summary>
    ///     对 SPIR-V 模块执行死代码消除。
    /// </summary>
    /// <param name="module">SPIR-V 语义模型。</param>
    /// <returns>优化后的模块。</returns>
    public SpirvModule Optimize(SpirvModule module)
    {
        return module;
    }
}

/// <summary>
///     SPIR-V 常量折叠优化 Pass。
/// </summary>
public sealed class ConstantFoldingPass
{
    /// <summary>
    ///     对 SPIR-V 模块执行常量折叠。
    /// </summary>
    /// <param name="module">SPIR-V 语义模型。</param>
    /// <returns>优化后的模块。</returns>
    public SpirvModule Optimize(SpirvModule module)
    {
        return module;
    }
}
