using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

/// <summary>
/// 表示索引访问表达式节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// arr[0]              // 数组索引访问
/// arr[i]              // 变量索引访问
/// matrix[row][col]    // 多维数组访问
/// list[index]         // 列表索引访问
/// dict["key"]         // 字典键访问
/// </code>
/// </remarks>
public sealed record TermIndexExpression(
    AstNode Object,
    AstNode Index,
    SourceSpan? Span = null) : AstNode(NodeType.IndexExpr, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitIndexExpr(this);
}
