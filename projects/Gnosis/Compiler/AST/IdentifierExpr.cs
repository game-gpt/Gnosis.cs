namespace Gnosis.Compiler.AST;

public sealed record IdentifierExpr(
    SourceSpan? Span,
    string Name) : AstNode(NodeType.IdentifierExpr, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIdentifierExpr(this);
}
