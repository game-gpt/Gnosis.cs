namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示返回语句节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// return;                 // 无返回值
/// return 42;              // 返回数字
/// return x + y;           // 返回表达式结果
/// return foo();           // 返回函数调用结果
/// return { a: 1, b: 2 };  // 返回对象字面量
/// </code>
/// </remarks>
public sealed record ReturnStmt(
    SourceSpan? Span,
    AstNode? Value) : AstNode(NodeType.ReturnStmt, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitReturnStmt(this);
}
