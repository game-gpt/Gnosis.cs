using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public sealed record ForStmt(
    SourceSpan? Span,
    AstNode? Initializer,
    AstNode? Condition,
    AstNode? Update,
    BlockStmt Body) : AstNode(NodeType.ForStmt, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitForStmt(this);
}
