namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示代码块语句节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// {                        // 空代码块
/// }
/// 
/// {                        // 包含语句的代码块
///     let x = 10;
///     foo();
/// }
/// 
/// if (condition) {         // 作为控制流主体
///     doSomething();
/// }
/// </code>
/// </remarks>
public sealed record BlockStmt(
    SourceSpan? Span,
    IReadOnlyList<AstNode> Statements) : AstNode(NodeType.BlockStmt, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBlockStmt(this);
}
