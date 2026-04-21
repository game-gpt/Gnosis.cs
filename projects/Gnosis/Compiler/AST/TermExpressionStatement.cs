namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示表达式语句节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// foo();                  // 函数调用语句
/// x = 10;                 // 赋值语句
/// obj.method();           // 方法调用语句
/// counter++;              // 自增语句
/// print("hello");         // 打印语句
/// </code>
/// </remarks>
public sealed record TermExpressionStatement(
    SourceSpan? Span,
    AstNode Expression) : AstNode(NodeType.ExprStmt, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitExprStmt(this);
}