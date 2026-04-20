using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public sealed record SystemDecl(
    SourceSpan? Span,
    string Name,
    IReadOnlyList<AttributeDecl> Attributes,
    IReadOnlyList<QueryExpr> Queries,
    IReadOnlyList<FunctionDecl> LifecycleMethods) : AstNode(NodeType.SystemDecl, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitSystemDecl(this);
}
