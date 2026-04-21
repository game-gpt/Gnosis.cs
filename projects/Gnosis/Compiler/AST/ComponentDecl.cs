namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示 ECS 组件声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// component Position {               // 简单组件
///     x: float;
///     y: float;
/// }
/// 
/// component Velocity {               // 带默认值的组件
///     dx: float = 0.0;
///     dy: float = 0.0;
/// }
/// 
/// [serialize]                        // 带属性的组件
/// component Health {
///     current: int;
///     max: int;
/// }
/// </code>
/// </remarks>
public sealed record ComponentDecl(
    SourceSpan? Span,
    string Name,
    IReadOnlyList<AttributeDecl> Attributes,
    IReadOnlyList<FieldDecl> Fields) : AstNode(NodeType.ComponentDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitComponentDecl(this);
}
