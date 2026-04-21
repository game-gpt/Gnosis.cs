namespace Gnosis.Compiler.AST;

public sealed record StructDecl(
    SourceSpan? Span,
    string Name,
    IReadOnlyList<FieldDecl> Fields,
    IReadOnlyList<AttributeDecl> Attributes) : AstNode(NodeType.StructDecl, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitStructDecl(this);
}
