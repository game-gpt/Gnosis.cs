namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示 ECS 系统声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// system MovementSystem {
///     # 查询声明
///     query all(Position, Velocity)
///     
///     # 生命周期方法
///     fn onInit() { }
///     
///     fn onUpdate(dt: float) {
///         for (entity in query) {
///             entity.position.x += entity.velocity.dx * dt
///         }
///     }
///     
///     fn onDestroy() { }
/// }
/// 
/// # 带属性的系统
/// [priority(100)]
/// system RenderSystem {
///     query all(Position, Sprite)
///     
///     fn onUpdate(dt: float) { }
/// }
/// </code>
/// </remarks>
public sealed record SystemDecl(
    SourceSpan? Span,
    string Name,
    IReadOnlyList<AttributeDecl> Attributes,
    IReadOnlyList<QueryExpr> Queries,
    IReadOnlyList<FunctionDecl> LifecycleMethods) : AstNode(NodeType.SystemDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitSystemDecl(this);
}
