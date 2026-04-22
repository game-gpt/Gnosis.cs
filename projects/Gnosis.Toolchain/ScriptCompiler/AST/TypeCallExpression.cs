using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

public sealed record TypeCallExpression(
    AstNode Callee,
    IReadOnlyList<AstNode> Arguments,
    SourceSpan? Span = null) : AstNode(NodeType.CallExpr, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitCallExpr(this);
}