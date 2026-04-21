namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示 for-each 循环语句节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// for (item in items) {             // 遍历集合
///     print(item);
/// }
/// 
/// for (i, item in items) {          // 带索引遍历
///     print(i, item);
/// }
/// 
/// for (char in "hello") {           // 遍历字符串
///     print(char);
/// }
/// 
/// for (key, value in dict) {        // 遍历字典
///     print(key, value);
/// }
/// </code>
/// </remarks>
public sealed record LoopStmt(
    SourceSpan? Span,
    string? IteratorName,
    AstNode? Iterable,
    BlockStmt Body) : AstNode(NodeType.LoopStmt, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLoopStmt(this);
}
