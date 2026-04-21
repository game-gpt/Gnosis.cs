namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示元数据块节点（用于嵌入原生代码或特殊内容）
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// meta {
///     #version 450
///     layout(location = 0) in vec3 position;
///     layout(location = 1) in vec2 uv;
/// }
/// 
/// meta_expr {
///     __builtin_shader_code__
/// }
/// </code>
/// </remarks>
public sealed record MetaBlock(
    SourceSpan? Span,
    string Content,
    bool IsExpression) : AstNode(NodeType.MetaBlock, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitMetaBlock(this);
}
