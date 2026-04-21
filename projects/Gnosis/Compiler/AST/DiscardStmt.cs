namespace Gnosis.Compiler.AST;

public sealed record DiscardStmt(
    SourceSpan? Span) : AstNode(NodeType.DiscardStmt, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitDiscardStmt(this);
}
