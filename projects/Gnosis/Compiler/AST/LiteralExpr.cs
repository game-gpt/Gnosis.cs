namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示字面量类型
/// </summary>
public enum LiteralType
{
    /// <summary>
    /// 数字字面量（整数或浮点数）
    /// </summary>
    Number,

    /// <summary>
    /// 字符串字面量
    /// </summary>
    String,

    /// <summary>
    /// 布尔字面量（true 或 false）
    /// </summary>
    Boolean,

    /// <summary>
    /// 空值字面量（null）
    /// </summary>
    Null
}

/// <summary>
/// 表示字面量表达式节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 整数字面量
/// 42
/// 
/// # 浮点数字面量
/// 3.14
/// 
/// # 字符串字面量
/// "hello"
/// 
/// # 布尔字面量（真）
/// true
/// 
/// # 布尔字面量（假）
/// false
/// 
/// # 空值字面量
/// null
/// </code>
/// </remarks>
public sealed record LiteralExpr(
    SourceSpan? Span,
    LiteralType LiteralKind,
    object? Value) : AstNode(NodeType.LiteralExpr, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitLiteralExpr(this);
}
