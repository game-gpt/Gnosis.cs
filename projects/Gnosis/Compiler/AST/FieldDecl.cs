namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示字段声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// struct Example {
///     x: int;                           // 简单字段
///     name: string = "default";         // 带默认值的字段
///     [range(0, 100)]                   // 带属性的字段
///     value: float = 0.0;
///     data: vec3[];                     // 数组类型字段
/// }
/// </code>
/// </remarks>
public sealed record FieldDecl(
    SourceSpan? Span,
    string Name,
    TypeAnnotation FieldType,
    AstNode? DefaultValue,
    IReadOnlyList<AttributeDecl> Attributes) : AstNode(NodeType.FieldDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitFieldDecl(this);
}
