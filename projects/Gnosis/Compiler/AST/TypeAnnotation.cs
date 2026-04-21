namespace Gnosis.Compiler.AST;

public sealed record TypeAnnotation(
    SourceSpan? Span,
    string Name,
    IReadOnlyList<TypeAnnotation> GenericArguments) : AstNode(NodeType.TypeAnnotation, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitTypeAnnotation(this);
}
