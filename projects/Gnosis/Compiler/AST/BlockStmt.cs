namespace Gnosis.Compiler.AST;

public sealed record BlockStmt(
    SourceSpan? Span,
    IReadOnlyList<AstNode> Statements) : AstNode(NodeType.BlockStmt, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBlockStmt(this);
}
