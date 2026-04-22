using Gnosis.Core.Diagnostic;

namespace Gnosis.Toolchain.ScriptCompiler.AST;

/// <summary>
/// 表示 GGScript/GGShader 特性标注声明节点（非 C# System.Attribute）
/// </summary>
/// <remarks>
/// <para>注意：此处的特性标注是 GG 语言的语法，与 C# 的 System.Attribute 无关。</para>
/// 语法示例：
/// <code>
/// # 无参数特性标注
/// [inline]
/// micro fast_func { }
/// 
/// # 带命名参数的特性标注
/// [range(min = 0, max = 100)]
/// let value: int
/// 
/// # 带字符串参数的特性标注
/// [deprecated("use new_func instead")]
/// micro old_func { }
/// 
/// # 多个特性标注
/// [serialize, json]
/// struct Data {
///     name: string
/// }
/// </code>
/// </remarks>
public sealed record AttributeDecl(
    string Name,
    IReadOnlyList<KeyValuePair<string, string>> Arguments,
    SourceSpan? Span = null) : AstNode(NodeType.AttributeDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitAttributeDecl(this);
}
