namespace Gnosis.Compiler.AST;

public sealed record ComponentDecl(
    SourceSpan? Span,
    string Name,
    IReadOnlyList<AttributeDecl> Attributes,
    IReadOnlyList<FieldDecl> Fields) : AstNode(NodeType.ComponentDecl, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitComponentDecl(this);
}
