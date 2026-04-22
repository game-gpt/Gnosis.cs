using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

/// <summary>
/// 表示结构体声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 简单结构体
/// struct Point {
///     x: float
///     y: float
/// }
/// 
/// # 带默认值的结构体
/// struct Person {
///     name: string
///     age: int = 0
///     active: bool = true
/// }
/// 
/// # 带 GGShader 特性标注的结构体
/// [packed]
/// struct Vertex {
///     position: vec3
///     normal: vec3
///     uv: vec2
/// }
/// </code>
/// </remarks>
public sealed record StructDecl(
    string Name,
    IReadOnlyList<FieldDecl> Fields,
    IReadOnlyList<AttributeDecl> Attributes,
    SourceSpan? Span = null) : AstNode(NodeType.StructDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitStructDecl(this);
}
