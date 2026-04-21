namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示标识符表达式节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// x              // 变量引用
/// myVariable     // 变量引用
/// MyFunction     // 函数引用
/// TypeName       // 类型引用
/// _              // 丢弃模式
/// </code>
/// </remarks>
public sealed record IdentifierExpr(
    SourceSpan? Span,
    string Name) : AstNode(NodeType.IdentifierExpr, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIdentifierExpr(this);
}
