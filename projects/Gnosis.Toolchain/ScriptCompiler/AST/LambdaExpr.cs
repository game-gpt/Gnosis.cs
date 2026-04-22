using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

/// <summary>
/// 表示 Lambda 表达式节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 无参数 Lambda
/// () =&gt; 42
/// 
/// # 单参数 Lambda
/// x =&gt; x * 2
/// 
/// # 多参数 Lambda
/// (x, y) =&gt; x + y
/// 
/// # 带类型注解的 Lambda
/// (a: int, b: int) =&gt; a + b
/// 
/// # 带函数体的 Lambda
/// (x) =&gt; {
///     let y = x * 2
///     return y
/// }
/// </code>
/// </remarks>
public sealed record LambdaExpr(
    IReadOnlyList<ParameterDecl> Parameters,
    AstNode Body,
    SourceSpan? Span = null) : AstNode(NodeType.LambdaExpr, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLambdaExpr(this);
}
