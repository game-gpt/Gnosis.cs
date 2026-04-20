using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public sealed record CallExpr(
    SourceSpan? Span,
    AstNode Callee,
    IReadOnlyList<AstNode> Arguments) : AstNode(NodeType.CallExpr, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitCallExpr(this);
}
