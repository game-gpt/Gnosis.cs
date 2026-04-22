using Gnosis.IR.Graph;
using Gnosis.IR.Instruction;
using Gnosis.ECS.World;

namespace Gnosis.Toolchain.Compiler;

/// <summary>
///     Gnosis 编译器后端接口，将 Nyar IR 编译为 Gnosis 引擎可执行的字节码
/// </summary>
public interface IGnosisCompilerBackend
{
    /// <summary>
    ///     后端名称
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     编译 IR 模块为 Gnosis 字节码单元
    /// </summary>
    /// <param name="module">Nyar IR 模块</param>
    /// <param name="options">编译选项</param>
    /// <returns>编译生成的字节码单元</returns>
    BytecodeUnit Compile(IrModule module, GnosisCompileOptions options);

    /// <summary>
    ///     验证 IR 模块是否可以被此后端编译
    /// </summary>
    /// <param name="module">Nyar IR 模块</param>
    /// <param name="diagnostics">验证产生的诊断信息列表</param>
    /// <returns>验证是否通过</returns>
    bool Validate(IrModule module, out List<string> diagnostics);
}

/// <summary>
///     Gnosis 编译选项
/// </summary>
public sealed class GnosisCompileOptions
{
    /// <summary>
    ///     是否生成文本输出（如 WAT、IL 文本等）
    /// </summary>
    public bool GenerateTextOutput { get; set; }

    /// <summary>
    ///     优化级别（0-3）
    /// </summary>
    public int OptimizationLevel { get; set; }

    /// <summary>
    ///     是否启用调试信息
    /// </summary>
    public bool GenerateDebugInfo { get; set; }

    /// <summary>
    ///     目标 ECS 世界（可选，用于 ECS 相关编译）
    /// </summary>
    public IWorld? TargetWorld { get; set; }
}
