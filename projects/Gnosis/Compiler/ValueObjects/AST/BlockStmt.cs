using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public sealed record BlockStmt(
    SourceSpan? Span,
    IReadOnlyList<AstNode> Statements) : AstNode(NodeType.BlockStmt, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBlockStmt(this);
}
