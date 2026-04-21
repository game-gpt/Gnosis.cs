namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示赋值表达式节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// x = 10           // 简单赋值
/// x += 5           // 复合赋值（加法）
/// x -= 3           // 复合赋值（减法）
/// x *= 2           // 复合赋值（乘法）
/// x /= 4           // 复合赋值（除法）
/// arr[0] = 100     // 索引赋值
/// obj.field = 42   // 成员赋值
/// </code>
/// </remarks>
public sealed record AssignmentExpr(
    SourceSpan? Span,
    AstNode Target,
    string Operator,
    AstNode Value) : AstNode(NodeType.AssignmentExpr, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitAssignmentExpr(this);
}
