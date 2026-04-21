namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示成员访问表达式节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// obj.field           // 字段访问
/// obj.method()        // 方法访问
/// Math.PI             // 静态成员访问
/// person.name         // 属性访问
/// vector.x            // 向量分量访问
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
