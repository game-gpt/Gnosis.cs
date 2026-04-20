using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public sealed record AssignmentExpr(
    SourceSpan? Span,
    AstNode Target,
    string Operator,
    AstNode Value) : AstNode(NodeType.AssignmentExpr, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitAssignmentExpr(this);
}
