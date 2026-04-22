using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

/// <summary>
/// 表示插件声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// plugin MathPlugin {
///     # 架构要求
///     requires: ["x86_64", "sse2"]
///     
///     # 提供的宏
///     provides_macros: ["vec_add", "vec_mul"]
///     
///     # 提供的能力
///     provides_capabilities: ["simd"]
///     
///     # 提供的函数
///     micro vec_add(a: vec4, b: vec4): vec4 {
///         return a + b
///     }
///     
///     micro vec_mul(a: vec4, b: vec4): vec4 {
///         return a * b
///     }
/// }
/// 
/// plugin AudioPlugin {
///     requires: ["audio_api"]
///     provides_capabilities: ["playback", "recording"]
///     
///     micro play_sound(path: string) { }
///     micro stop_sound { }
/// }
/// </code>
/// </remarks>
public sealed record PluginDecl(
    string Name,
    IReadOnlyList<string> RequiresArch,
    IReadOnlyList<string> ProvidesMacros,
    IReadOnlyList<string> ProvidesCapabilities,
    IReadOnlyList<FunctionDecl> Functions,
    SourceSpan? Span = null) : AstNode(NodeType.PluginDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitPluginDecl(this);
}
