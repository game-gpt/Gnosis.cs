namespace Gnosis.Compiler.AST;

public sealed record ImportDecl(
    SourceSpan? Span,
    string ModulePath,
    string? Alias = null) : AstNode(NodeType.ImportDecl, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitImportDecl(this);
}
