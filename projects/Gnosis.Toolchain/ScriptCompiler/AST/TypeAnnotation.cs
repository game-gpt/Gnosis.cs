using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

/// <summary>
/// 表示类型注解节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// # 简单类型
/// let x: int
/// 
/// # 数组类型
/// let arr: int[]
/// 
/// # 泛型类型
/// let map: Map&lt;string, int&gt;
/// 
/// # 函数类型
/// let func: (int, int) =&gt; int
/// 
/// # 内置类型
/// let vec: vec3
/// 
/// # 可选类型
/// let opt: Option&lt;string&gt;
/// </code>
/// </remarks>
public sealed record TypeAnnotation(
    string Name,
    IReadOnlyList<TypeAnnotation> GenericArguments,
    SourceSpan? Span = null) : AstNode(NodeType.TypeAnnotation, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitTypeAnnotation(this);
}
