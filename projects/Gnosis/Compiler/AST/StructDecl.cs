namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示结构体声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// struct Point {                // 简单结构体
///     x: float;
///     y: float;
/// }
/// 
/// struct Person {               // 带默认值的结构体
///     name: string;
///     age: int = 0;
///     active: bool = true;
/// }
/// 
/// [packed]                      // 带属性的结构体
/// struct Vertex {
///     position: vec3;
///     normal: vec3;
///     uv: vec2;
/// }
/// </code>
/// </remarks>
public sealed record StructDecl(
    SourceSpan? Span,
    string Name,
    IReadOnlyList<FieldDecl> Fields,
    IReadOnlyList<AttributeDecl> Attributes) : AstNode(NodeType.StructDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitStructDecl(this);
}
