using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public sealed record UnaryExpr(
    SourceSpan? Span,
    string Operator,
    AstNode Operand,
    bool IsPrefix) : AstNode(NodeType.UnaryExpr, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUnaryExpr(this);
}
