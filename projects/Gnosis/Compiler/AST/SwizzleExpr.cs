namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示向量分量重组表达式节点（Swizzle）
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// vec.xyz         // 提取 x、y、z 分量
/// vec.rgb         // 提取 r、g、b 分量（颜色）
/// vec.xy          // 提取 x、y 分量
/// vec.xxx         // 重复 x 分量
/// vec.zyx         // 反转分量顺序
/// vec.rgba        // 提取所有颜色分量
/// </code>
/// </remarks>
public sealed record SwizzleExpr(
    SourceSpan? Span,
    AstNode Object,
    string Components) : AstNode(NodeType.SwizzleExpr, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitSwizzleExpr(this);
}
