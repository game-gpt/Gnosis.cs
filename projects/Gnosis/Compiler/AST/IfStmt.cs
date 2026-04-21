namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示条件语句节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// if (x &gt; 0) {              // 简单 if 语句
///     doSomething();
/// }
/// 
/// if (x &gt; 0) {              // if-else 语句
///     doA();
/// } else {
///     doB();
/// }
/// 
/// if (x &gt; 0) {              // if-else if-else 链
///     doA();
/// } else if (x &lt; 0) {
///     doB();
/// } else {
///     doC();
/// }
/// </code>
/// </remarks>
public sealed record IfStmt(
    SourceSpan? Span,
    AstNode Condition,
    AstNode ThenBlock,
    AstNode? ElseBlock) : AstNode(NodeType.IfStmt, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIfStmt(this);
}
