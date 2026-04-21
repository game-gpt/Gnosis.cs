namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示特性声明节点（用于添加元数据）
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 无参数特性
/// [inline]
/// micro fast_func { }
/// 
/// # 带命名参数的特性
/// [range(min = 0, max = 100)]
/// let value: int
/// 
/// # 带字符串参数的特性
/// [deprecated("use new_func instead")]
/// micro old_func { }
/// 
/// # 多个特性
/// [serialize, json]
/// struct Data {
///     name: string
/// }
/// </code>
/// </remarks>
public sealed record AttributeDecl(
    SourceSpan? Span,
    string Name,
    IReadOnlyList<KeyValuePair<string, string>> Arguments) : AstNode(NodeType.AttributeDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitAttributeDecl(this);
}
