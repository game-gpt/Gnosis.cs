namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示类型注解节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// let x: int;                          // 简单类型
/// let arr: int[];                      // 数组类型
/// let map: Map&lt;string, int&gt;;           // 泛型类型
/// let func: (int, int) =&gt; int;         // 函数类型
/// let vec: vec3;                       // 内置类型
/// let opt: Option&lt;string&gt;;             // 可选类型
/// </code>
/// </remarks>
public sealed record TypeAnnotation(
    SourceSpan? Span,
    string Name,
    IReadOnlyList<TypeAnnotation> GenericArguments) : AstNode(NodeType.TypeAnnotation, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitTypeAnnotation(this);
}
