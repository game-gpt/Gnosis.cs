namespace Gnosis.Compiler.AST;

public sealed record TypeExpressionStatement(
    SourceSpan? Span,
    AstNode Expression) : AstNode(NodeType.ExprStmt, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitExprStmt(this);
}