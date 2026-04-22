using Gnosis.Core.Diagnostic;

namespace Gnosis.Compiler.AST;

public sealed record TypeExpressionStatement(
    AstNode Expression,
    SourceSpan? Span = null) : AstNode(NodeType.ExprStmt, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitExprStmt(this);
}