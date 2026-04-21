namespace Gnosis.Compiler.AST;

/// <summary>
/// 表示 UI 组件声明节点
/// </summary>
/// <remarks>
/// 语法示例：
/// <code>
/// widget Button {                        // 简单按钮组件
///     text: string;
///     onClick: () =&gt; void;
///     
///     fn render() {
///         &lt;button onclick={onClick}&gt;{text}&lt;/button&gt;
///     }
/// }
/// 
/// widget Slider {                        // 滑动条组件
///     value: float;
///     min: float = 0.0;
///     max: float = 100.0;
///     onChange: (float) =&gt; void;
///     
///     fn render() {
///         &lt;input type="range" min={min} max={max} value={value} onchange={onChange} /&gt;
///     }
/// }
/// </code>
/// </remarks>
public sealed record WidgetDecl(
    SourceSpan? Span,
    string Name,
    IReadOnlyList<FieldDecl> Properties,
    FunctionDecl? RenderMethod) : AstNode(NodeType.WidgetDecl, Span)
{
    /// <summary>
    /// 接受访问者访问
    /// </summary>
    public override T Accept<T>(IAstVisitor<T> visitor) => visitor.VisitWidgetDecl(this);
}
