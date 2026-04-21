using Gnosis.Compiler.AST;
using Gnosis.Compiler.Diagnostics;

namespace Gnosis.Compiler.Meta;

/// <summary>
/// 编译期钩子接口
/// </summary>
/// <remarks>
/// 实现此接口以在编译期拦截特定类型的操作。
/// 例如，swizzle 表达式可以通过 MemberAccess 钩子实现。
/// </remarks>
public interface ICompileTimeHook
{
    /// <summary>
    /// 钩子类型
    /// </summary>
    CompileTimeHookKind Kind { get; }

    /// <summary>
    /// 目标类型名称（支持泛型，如 vec4）
    /// </summary>
    string TargetTypeName { get; }

    /// <summary>
    /// 钩子方法名（如 __resolve_member）
    /// </summary>
    string MethodName { get; }

    /// <summary>
    /// 执行钩子
    /// </summary>
    /// <param name="context">编译期上下文</param>
    /// <param name="args">钩子参数</param>
    /// <returns>替换后的 AST 节点，返回 null 表示回退到默认行为</returns>
    AstNode? Execute(CompileTimeContext context, params object[] args);
}

/// <summary>
/// 编译期上下文
/// </summary>
public sealed record CompileTimeContext(
    string FilePath,
    DiagnosticSink Diagnostics,
    IMacroTable MacroTable)
{
    /// <summary>
    /// 当前编译期变量绑定
    /// </summary>
    public Dictionary<string, object> Variables { get; } = new();
}
