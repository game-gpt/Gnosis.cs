using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public sealed record FunctionDecl(
    SourceSpan? Span,
    string Name,
    IReadOnlyList<ParameterDecl> Parameters,
    TypeAnnotation? ReturnType,
    BlockStmt? Body,
    IReadOnlyList<AttributeDecl> Attributes) : AstNode(NodeType.FunctionDecl, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitFunctionDecl(this);
}
