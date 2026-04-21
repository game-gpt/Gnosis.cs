namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示参数声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 简单参数
/// fn foo(x: int) { }
/// 
/// # 多参数
/// fn bar(a: int, b: string) { }
/// 
/// # 数组类型参数
/// fn baz(data: vec3[]) { }
/// 
/// # 带属性的参数
/// fn qux([range(0, 10)] value: int) { }
/// </code>
/// </remarks>
public sealed record ParameterDecl(
    SourceSpan? Span,
    string Name,
    TypeAnnotation ParamType,
    IReadOnlyList<AttributeDecl> Attributes) : AstNode(NodeType.ParameterDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitParameterDecl(this);
}
