using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

/// <summary>
/// 表示 for-each 循环语句节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 遍历集合
/// for item in items {
///     print(item)
/// }
/// 
/// # 带索引遍历
/// for i, item in items {
///     print(i, item)
/// }
/// 
/// # 遍历字符串
/// for char in "hello" {
///     print(char)
/// }
/// 
/// # 遍历字典
/// for key, value in dict {
///     print(key, value)
/// }
/// </code>
/// </remarks>
public sealed record LoopStmt(
    string? IteratorName,
    AstNode? Iterable,
    BlockStmt Body,
    SourceSpan? Span = null) : AstNode(NodeType.LoopStmt, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLoopStmt(this);
}
