using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

/// <summary>
/// 表示查询类型
/// </summary>
public enum QueryKind
{
    /// <summary>
    /// 查询所有匹配的实体
    /// </summary>
    All,

    /// <summary>
    /// 查询是否存在任意匹配的实体
    /// </summary>
    Any,

    /// <summary>
    /// 查询是否没有匹配的实体
    /// </summary>
    None
}

/// <summary>
/// 表示实体查询表达式节点（用于 ECS 系统）
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 查询所有拥有 Position 和 Velocity 组件的实体
/// query all(Position, Velocity)
/// 
/// # 查询是否存在拥有 Health 组件的实体
/// query any(Health)
/// 
/// # 查询是否没有 Dead 组件的实体
/// query none(Dead)
/// 
/// # 带过滤器的查询
/// query all(A, B).filter(any(C))
/// </code>
/// </remarks>
public sealed record QueryExpr(
    QueryKind Kind,
    IReadOnlyList<TypeAnnotation> ComponentTypes,
    IReadOnlyList<QueryExpr>? Filters = null,
    SourceSpan? Span = null) : AstNode(NodeType.QueryExpr, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitQueryExpr(this);
}
