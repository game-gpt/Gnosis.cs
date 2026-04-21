namespace Gnosis.Compiler.AST;

public sealed record IndexExpr(
    SourceSpan? Span,
    AstNode Object,
    AstNode Index) : AstNode(NodeType.IndexExpr, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIndexExpr(this);
}
