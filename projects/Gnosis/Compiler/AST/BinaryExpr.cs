namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示二元表达式节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// a + b            // 加法
/// a - b            // 减法
/// a * b            // 乘法
/// a / b            // 除法
/// a % b            // 取模
/// a == b           // 相等比较
/// a != b           // 不等比较
/// a &lt; b           // 小于
/// a &lt;= b          // 小于等于
/// a &gt; b           // 大于
/// a &gt;= b          // 大于等于
/// a &amp;&amp; b          // 逻辑与
/// a || b           // 逻辑或
/// </code>
/// </remarks>
public sealed record BinaryExpr(
    SourceSpan? Span,
    AstNode Left,
    string Operator,
    AstNode Right) : AstNode(NodeType.BinaryExpr, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitBinaryExpr(this);
}
