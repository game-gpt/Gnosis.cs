namespace Gnosis.Compiler.AST;

public sealed record LambdaExpr(
    SourceSpan? Span,
    IReadOnlyList<ParameterDecl> Parameters,
    AstNode Body) : AstNode(NodeType.LambdaExpr, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLambdaExpr(this);
}
