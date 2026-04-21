namespace Gnosis.Compiler.AST;

public sealed record ReturnStmt(
    SourceSpan? Span,
    AstNode? Value) : AstNode(NodeType.ReturnStmt, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitReturnStmt(this);
}
