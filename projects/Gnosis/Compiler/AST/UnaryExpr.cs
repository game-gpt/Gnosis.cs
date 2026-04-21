namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示一元表达式节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// -x              // 负号（前缀）
/// !flag           // 逻辑非（前缀）
/// ++i             // 前缀自增
/// --i             // 前缀自减
/// i++             // 后缀自增
/// i--             // 后缀自减
/// </code>
/// </remarks>
public sealed record UnaryExpr(
    SourceSpan? Span,
    string Operator,
    AstNode Operand,
    bool IsPrefix) : AstNode(NodeType.UnaryExpr, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUnaryExpr(this);
}
