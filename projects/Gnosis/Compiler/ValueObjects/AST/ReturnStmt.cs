using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public sealed record ReturnStmt(
    SourceSpan? Span,
    AstNode? Value) : AstNode(NodeType.ReturnStmt, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitReturnStmt(this);
}
