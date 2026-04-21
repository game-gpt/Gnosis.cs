namespace Gnosis.Compiler.AST;

public sealed record BinaryExpr(
    SourceSpan? Span,
    AstNode Left,
    string Operator,
    AstNode Right) : AstNode(NodeType.BinaryExpr, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBinaryExpr(this);
}
