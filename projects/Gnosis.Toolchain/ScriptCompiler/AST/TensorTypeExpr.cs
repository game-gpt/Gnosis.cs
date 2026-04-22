using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

/// <summary>
/// 表示张量类型表达式节点，用于 neural 块中的权重声明
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 二维张量（矩阵）
/// weight: tensor&lt;f16, [out_dim, in_dim]&gt;
///
/// # 一维张量（向量）
/// bias: tensor&lt;f32, [128]&gt;
///
/// # 四维张量（卷积核）
/// kernel: tensor&lt;f16, [64, 3, 3, 3]&gt;
///
/// # 动态维度
/// dynamic: tensor&lt;f32, [?, 768]&gt;
/// </code>
/// </remarks>
public sealed record TensorTypeExpr(
    TypeAnnotation ElementType,
    IReadOnlyList<TensorDimension> Dimensions,
    SourceSpan? Span = null) : AstNode(NodeType.TensorTypeExpr, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitTensorTypeExpr(this);
}

/// <summary>
/// 表示张量维度，支持静态维度和动态维度
/// </summary>
public sealed record TensorDimension(
    bool IsDynamic,
    string? StaticValue,
    string? DynamicName,
    SourceSpan? Span = null) : AstNode(NodeType.TensorDimension, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitTensorDimension(this);
}
