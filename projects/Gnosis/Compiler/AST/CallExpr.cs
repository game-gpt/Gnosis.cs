namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示函数调用表达式节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// foo()                    // 无参数调用
/// bar(1, 2, 3)             // 多参数调用
/// obj.method()             // 成员方法调用
/// func(a, b, c)            // 变量参数调用
/// Math.sqrt(16)            // 静态方法调用
/// lambda(x, y)             // Lambda 调用
/// </code>
/// </remarks>
public sealed record CallExpr(
    SourceSpan? Span,
    AstNode Callee,
    IReadOnlyList<AstNode> Arguments) : AstNode(NodeType.CallExpr, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitCallExpr(this);
}
