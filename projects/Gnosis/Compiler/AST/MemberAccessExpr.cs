namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示成员访问表达式节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 字段访问
/// obj.field
/// 
/// # 方法访问
/// obj.method()
/// 
/// # 静态成员访问
/// Math.PI
/// 
/// # 属性访问
/// person.name
/// 
/// # 向量分量访问
/// vector.x
/// </code>
/// </remarks>
public sealed record MemberAccessExpr(
    SourceSpan? Span,
    AstNode Object,
    string MemberName) : AstNode(NodeType.MemberAccessExpr, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMemberAccessExpr(this);
}
