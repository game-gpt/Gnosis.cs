using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public sealed record FieldDecl(
    SourceSpan? Span,
    string Name,
    TypeAnnotation FieldType,
    AstNode? DefaultValue,
    IReadOnlyList<AttributeDecl> Attributes) : AstNode(NodeType.FieldDecl, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitFieldDecl(this);
}
