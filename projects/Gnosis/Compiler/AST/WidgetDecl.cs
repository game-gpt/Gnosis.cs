namespace Gnosis.Compiler.AST;

public sealed record WidgetDecl(
    SourceSpan? Span,
    string Name,
    IReadOnlyList<FieldDecl> Properties,
    FunctionDecl? RenderMethod) : AstNode(NodeType.WidgetDecl, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitWidgetDecl(this);
}
