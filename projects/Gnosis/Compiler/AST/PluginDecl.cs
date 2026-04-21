namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示插件声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// plugin MathPlugin {
///     requires: ["x86_64", "sse2"];          // 架构要求
///     provides_macros: ["vec_add", "vec_mul"]; // 提供的宏
///     provides_capabilities: ["simd"];       // 提供的能力
///     
///     fn vecAdd(a: vec4, b: vec4): vec4 {    // 提供的函数
///         return a + b;
///     }
///     
///     fn vecMul(a: vec4, b: vec4): vec4 {
///         return a * b;
///     }
/// }
/// 
/// plugin AudioPlugin {
///     requires: ["audio_api"];
///     provides_capabilities: ["playback", "recording"];
///     
///     fn playSound(path: string) { }
///     fn stopSound() { }
/// }
/// </code>
/// </remarks>
public sealed record PluginDecl(
    SourceSpan? Span,
    string Name,
    IReadOnlyList<string> RequiresArch,
    IReadOnlyList<string> ProvidesMacros,
    IReadOnlyList<string> ProvidesCapabilities,
    IReadOnlyList<FunctionDecl> Functions) : AstNode(NodeType.PluginDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitPluginDecl(this);
}
