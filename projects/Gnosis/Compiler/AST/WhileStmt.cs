namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示 while 循环语句节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 条件循环
/// while x &gt; 0 {
///     x--
/// }
/// 
/// # 无限循环
/// while true {
///     if done break
///     process()
/// }
/// 
/// # 带函数条件的循环
/// while hasNext() {
///     process(next())
/// }
/// </code>
/// </remarks>
public sealed record WhileStmt(
    SourceSpan? Span,
    AstNode Condition,
    BlockStmt Body) : AstNode(NodeType.WhileStmt, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitWhileStmt(this);
}
