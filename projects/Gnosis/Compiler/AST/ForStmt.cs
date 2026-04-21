namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示 for 循环语句节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 标准 for 循环
/// for (let i = 0; i &lt; 10; i++) {
///     print(i)
/// }
/// 
/// # 无限循环
/// for (;;) {
///     if (done) break
/// }
/// 
/// # 多变量循环
/// for (let i = 0, j = 10; i &lt; j; i++, j--) {
///     print(i, j)
/// }
/// </code>
/// </remarks>
public sealed record ForStmt(
    SourceSpan? Span,
    AstNode? Initializer,
    AstNode? Condition,
    AstNode? Update,
    BlockStmt Body) : AstNode(NodeType.ForStmt, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitForStmt(this);
}
