using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public sealed record MemberAccessExpr(
    SourceSpan? Span,
    AstNode Object,
    string MemberName) : AstNode(NodeType.MemberAccessExpr, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMemberAccessExpr(this);
}
