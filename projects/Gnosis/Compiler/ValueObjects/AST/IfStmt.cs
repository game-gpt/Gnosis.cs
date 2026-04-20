using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public sealed record IfStmt(
    SourceSpan? Span,
    AstNode Condition,
    AstNode ThenBlock,
    AstNode? ElseBlock) : AstNode(NodeType.IfStmt, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIfStmt(this);
}
