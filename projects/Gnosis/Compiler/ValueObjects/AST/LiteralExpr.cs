using Gnosis.Compiler.ValueObjects;

namespace Gnosis.Compiler.ValueObjects.AST;

public enum LiteralType
{
    Number,
    String,
    Boolean,
    Null
}

public sealed record LiteralExpr(
    SourceSpan? Span,
    LiteralType LiteralKind,
    object? Value) : AstNode(NodeType.LiteralExpr, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLiteralExpr(this);
}
