using Oak.GGScript.AST;

namespace Gnosis.Toolchain.ScriptCompiler.Meta;

/// <summary>
/// 内置 Swizzle 钩子
/// </summary>
/// <remarks>
/// 处理向量类型的 swizzle 表达式，支持 xyzw/rgba/stpq 三种分量命名集。
/// 
/// 示例用法：
/// <code>
/// let v: vec4&lt;f32&gt; = ...;
/// v.x      # 单分量提取 → float
/// v.xyz    # 多分量提取 → vec3&lt;f32&gt;
/// v.xxx    # 分量复制 → vec3&lt;f32&gt;
/// v.rgba   # 颜色分量 → vec4&lt;f32&gt;
/// </code>
/// </remarks>
public sealed class BuiltinSwizzleHook : ICompileTimeHook
{
    #region Properties

    public CompileTimeHookKind Kind => CompileTimeHookKind.MemberAccess;

    public string TargetTypeName { get; }

    public string MethodName => "__resolve_member";

    #endregion

    #region Constructors

    public BuiltinSwizzleHook(string targetTypeName)
    {
        TargetTypeName = targetTypeName;
    }

    #endregion

    #region ICompileTimeHook Implementation

    public AstNode? Execute(CompileTimeContext context, params object[] args)
    {
        if (args.Length < 2)
        {
            return null;
        }

        var obj = args[0] as AstNode;
        var memberName = args[1] as string;

        if (obj == null || memberName == null)
        {
            return null;
        }

        var components = ParseSwizzleComponents(memberName);
        if (components == null)
        {
            return null;
        }

        return new SwizzleExpr(obj, memberName);
    }

    #endregion

    #region Private Methods

    private static int[]? ParseSwizzleComponents(string memberName)
    {
        if (string.IsNullOrEmpty(memberName) || memberName.Length > 4)
        {
            return null;
        }

        var isXyzw = memberName.All(c => c is 'x' or 'y' or 'z' or 'w');
        var isRgba = memberName.All(c => c is 'r' or 'g' or 'b' or 'a');
        var isStpq = memberName.All(c => c is 's' or 't' or 'p' or 'q');

        if (!isXyzw && !isRgba && !isStpq)
        {
            return null;
        }

        return memberName.Select(c => c switch
        {
            'x' or 'r' or 's' => 0,
            'y' or 'g' or 't' => 1,
            'z' or 'b' or 'p' => 2,
            'w' or 'a' or 'q' => 3,
            _ => 0
        }).ToArray();
    }

    #endregion
}
