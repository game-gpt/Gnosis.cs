using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public sealed record WhileStmt(
    SourceSpan? Span,
    AstNode Condition,
    BlockStmt Body) : AstNode(NodeType.WhileStmt, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitWhileStmt(this);
}
