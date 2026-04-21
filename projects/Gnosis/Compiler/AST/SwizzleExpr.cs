namespace Gnosis.Compiler.AST;

public sealed record SwizzleExpr(
    SourceSpan? Span,
    AstNode Object,
    string Components) : AstNode(NodeType.SwizzleExpr, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitSwizzleExpr(this);
}
