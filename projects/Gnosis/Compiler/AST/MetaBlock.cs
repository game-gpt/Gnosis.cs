namespace Gnosis.Compiler.AST;

public sealed record MetaBlock(
    SourceSpan? Span,
    string Content,
    bool IsExpression) : AstNode(NodeType.MetaBlock, Span)
{
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMetaBlock(this);
}
