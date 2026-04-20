using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public enum QueryKind
{
    All,
    Any,
    None
}

public sealed record QueryExpr(
    SourceSpan? Span,
    QueryKind Kind,
    IReadOnlyList<TypeAnnotation> ComponentTypes,
    IReadOnlyList<QueryExpr>? Filters = null) : AstNode(NodeType.QueryExpr, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitQueryExpr(this);
}
