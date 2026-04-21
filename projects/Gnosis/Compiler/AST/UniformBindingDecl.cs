namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示 Uniform 绑定声明节点（用于 GPU 着色器）
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// uniform modelMatrix: mat4 @ (0, 0);       // group 0, binding 0
/// uniform viewMatrix: mat4 @ (0, 1);        // group 0, binding 1
/// uniform projectionMatrix: mat4 @ (0, 2);  // group 0, binding 2
/// 
/// uniform lights: Light[] @ (1, 0);         // 数组类型的 uniform
/// 
/// [readonly]                                // 带属性的 uniform
/// uniform textures: Texture2D @ (2, 0);
/// </code>
/// </remarks>
public sealed record UniformBindingDecl(
    SourceSpan? Span,
    string Name,
    string BindingType,
    TypeAnnotation TypeAnnotation,
    int? Group,
    int? Binding,
    IReadOnlyList<AttributeDecl> Attributes) : AstNode(NodeType.UniformBindingDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitUniformBindingDecl(this);
}
