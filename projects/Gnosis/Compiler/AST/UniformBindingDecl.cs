namespace Gnosis.Compiler.AST;

public sealed record UniformBindingDecl(
    SourceSpan? Span,
    string Name,
    string BindingType,
    TypeAnnotation TypeAnnotation,
    int? Group,
    int? Binding,
    IReadOnlyList<AttributeDecl> Attributes) : AstNode(NodeType.UniformBindingDecl, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUniformBindingDecl(this);
}
