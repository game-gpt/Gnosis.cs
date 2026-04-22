using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

/// <summary>
/// 表示神经层声明节点，定义可由 Tensor Core 执行的神经网络层
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// neural LinearLayer&lt;in_dim: u32, out_dim: u32&gt; @precision(half) {
///     weight: tensor&lt;f16, [out_dim, in_dim]&gt;,
///     bias: tensor&lt;f16, [out_dim]&gt;,
///
///     forward(input: vec&lt;in_dim, f32&gt;) -> vec&lt;out_dim, f32&gt; {
///         return matmul(weight, input) + bias;
///     }
/// }
/// </code>
/// </remarks>
public sealed record NeuralDecl(
    string Name,
    IReadOnlyList<ParameterDecl> GenericParameters,
    IReadOnlyList<FieldDecl> Weights,
    FunctionDecl ForwardFunction,
    IReadOnlyList<AttributeDecl> Attributes,
    SourceSpan? Span = null) : AstNode(NodeType.NeuralDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitNeuralDecl(this);
}
