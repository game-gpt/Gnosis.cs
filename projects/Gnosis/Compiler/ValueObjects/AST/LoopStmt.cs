using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public sealed record LoopStmt(
    SourceSpan? Span,
    string? IteratorName,
    AstNode? Iterable,
    BlockStmt Body) : AstNode(NodeType.LoopStmt, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLoopStmt(this);
}
