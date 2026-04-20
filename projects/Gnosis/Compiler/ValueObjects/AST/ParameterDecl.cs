using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public sealed record ParameterDecl(
    SourceSpan? Span,
    string Name,
    TypeAnnotation ParamType) : AstNode(NodeType.ParameterDecl, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitParameterDecl(this);
}
