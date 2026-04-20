using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public sealed record AttributeDecl(
    SourceSpan? Span,
    string Name,
    IReadOnlyList<KeyValuePair<string, string>> Arguments) : AstNode(NodeType.AttributeDecl, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitAttributeDecl(this);
}
