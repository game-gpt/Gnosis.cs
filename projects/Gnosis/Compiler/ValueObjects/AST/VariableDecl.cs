using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public sealed record VariableDecl(
    SourceSpan? Span,
    string Name,
    TypeAnnotation? VarType,
    AstNode? Initializer,
    bool IsMutable) : AstNode(NodeType.VariableDecl, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitVariableDecl(this);
}
