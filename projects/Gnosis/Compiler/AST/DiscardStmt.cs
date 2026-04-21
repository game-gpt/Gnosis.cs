namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示丢弃语句节点（用于丢弃不需要的返回值）
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// _ = foo();              // 丢弃函数返回值
/// _ = obj.property;       // 丢弃属性值
/// let (_, b) = tuple;     // 解构时丢弃部分值
/// </code>
/// </remarks>
public sealed record DiscardStmt(
    SourceSpan? Span) : AstNode(NodeType.DiscardStmt, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitDiscardStmt(this);
}
