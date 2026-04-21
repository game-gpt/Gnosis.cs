namespace Gnosis.Compiler.AST;

public sealed record ExprStmt(
    SourceSpan? Span,
    AstNode Expression) : AstNode(NodeType.ExprStmt, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitExprStmt(this);
}
