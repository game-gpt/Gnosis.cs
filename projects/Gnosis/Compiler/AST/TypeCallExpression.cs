namespace Gnosis.Compiler.AST;

public sealed record TypeCallExpression(
    SourceSpan? Span,
    AstNode Callee,
    IReadOnlyList<AstNode> Arguments) : AstNode(NodeType.CallExpr, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitCallExpr(this);
}