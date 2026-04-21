namespace Gnosis.Compiler.AST;

public sealed record UsingDecl(
    SourceSpan? Span,
    string NamespacePath,
    IReadOnlyList<string> Selections) : AstNode(NodeType.UsingDecl, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUsingDecl(this);
}
