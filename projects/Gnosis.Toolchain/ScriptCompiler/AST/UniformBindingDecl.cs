using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

/// <summary>
/// 表示 Uniform 绑定声明节点（用于 GPU 着色器）
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # group 0, binding 0
/// uniform model_matrix: mat4 @ (0, 0)
/// 
/// # group 0, binding 1
/// uniform view_matrix: mat4 @ (0, 1)
/// 
/// # group 0, binding 2
/// uniform projection_matrix: mat4 @ (0, 2)
/// 
/// # 数组类型的 uniform
/// uniform lights: Light[] @ (1, 0)
/// 
/// # 带 GGShader 特性标注的 uniform
/// [readonly]
/// uniform textures: Texture2D @ (2, 0)
/// </code>
/// </remarks>
public sealed record UniformBindingDecl(
    string Name,
    string BindingType,
    TypeAnnotation TypeAnnotation,
    int? Group,
    int? Binding,
    IReadOnlyList<AttributeDecl> Attributes,
    SourceSpan? Span = null) : AstNode(NodeType.UniformBindingDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUniformBindingDecl(this);
}
