namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示 Lambda 表达式节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// () =&gt; 42                        // 无参数 Lambda
/// x =&gt; x * 2                      // 单参数 Lambda
/// (x, y) =&gt; x + y                 // 多参数 Lambda
/// (a: int, b: int) =&gt; a + b       // 带类型注解的 Lambda
/// (x) =&gt; {                        // 带函数体的 Lambda
///     let y = x * 2;
///     return y;
/// }
/// </code>
/// </remarks>
public sealed record LambdaExpr(
    SourceSpan? Span,
    IReadOnlyList<ParameterDecl> Parameters,
    AstNode Body) : AstNode(NodeType.LambdaExpr, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLambdaExpr(this);
}
