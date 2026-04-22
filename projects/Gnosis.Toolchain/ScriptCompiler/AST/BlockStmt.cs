using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

/// <summary>
/// 表示代码块语句节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 空代码块
/// {
/// }
/// 
/// # 包含语句的代码块
/// {
///     let x = 10
///     foo()
/// }
/// 
/// # 作为控制流主体
/// if condition {
///     do_something()
/// }
/// </code>
/// </remarks>
public sealed record BlockStmt(
    IReadOnlyList<AstNode> Statements,
    SourceSpan? Span = null) : AstNode(NodeType.BlockStmt, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBlockStmt(this);
}
