using System.Collections.Concurrent;

namespace Gnosis.Toolchain.ScriptCompiler.Meta;

/// <summary>
/// 编译期钩子注册表
/// </summary>
/// <remarks>
/// 管理所有编译期钩子的注册和查找。
/// 支持内置钩子和用户自定义钩子。
/// </remarks>
public sealed class CompileTimeHookRegistry
{
    #region Fields

    private readonly ConcurrentDictionary<CompileTimeHookKind, ConcurrentDictionary<string, List<ICompileTimeHook>>> _hooks = new();

    #endregion

    #region Singleton

    private static readonly Lazy<CompileTimeHookRegistry> _instance = new(() => new CompileTimeHookRegistry());

    /// <summary>
    /// 全局实例
    /// </summary>
    public static CompileTimeHookRegistry Instance => _instance.Value;

    #endregion

    #region Constructors

    private CompileTimeHookRegistry()
    {
        RegisterBuiltinHooks();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 注册编译期钩子
    /// </summary>
    public void Register(ICompileTimeHook hook)
    {
        var kindDict = _hooks.GetOrAdd(hook.Kind, _ => new());
        var typeHooks = kindDict.GetOrAdd(hook.TargetTypeName, _ => []);
        lock (typeHooks)
        {
            typeHooks.Add(hook);
        }
    }

    /// <summary>
    /// 查找编译期钩子
    /// </summary>
    public IReadOnlyList<ICompileTimeHook> FindHooks(CompileTimeHookKind kind, string typeName)
    {
        if (_hooks.TryGetValue(kind, out var kindDict))
        {
            var result = new List<ICompileTimeHook>();

            if (kindDict.TryGetValue(typeName, out var exactHooks))
            {
                result.AddRange(exactHooks);
            }

            foreach (var (key, hooks) in kindDict)
            {
                if (IsGenericTypeMatch(key, typeName) && key != typeName)
                {
                    result.AddRange(hooks);
                }
            }

            return result;
        }

        return [];
    }

    /// <summary>
    /// 执行成员访问钩子
    /// </summary>
    public AstNode? ExecuteMemberAccessHook(
        string typeName,
        AstNode obj,
        string memberName,
        CompileTimeContext context)
    {
        var hooks = FindHooks(CompileTimeHookKind.MemberAccess, typeName);

        foreach (var hook in hooks)
        {
            var result = hook.Execute(context, obj, memberName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// 执行索引访问钩子
    /// </summary>
    public AstNode? ExecuteIndexAccessHook(
        string typeName,
        AstNode obj,
        AstNode index,
        CompileTimeContext context)
    {
        var hooks = FindHooks(CompileTimeHookKind.IndexAccess, typeName);

        foreach (var hook in hooks)
        {
            var result = hook.Execute(context, obj, index);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    #endregion

    #region Private Methods

    private static bool IsGenericTypeMatch(string pattern, string typeName)
    {
        if (pattern.EndsWith("<>"))
        {
            var baseName = pattern[..^2];
            return typeName.StartsWith(baseName);
        }

        return false;
    }

    private void RegisterBuiltinHooks()
    {
        Register(new BuiltinSwizzleHook("vec2"));
        Register(new BuiltinSwizzleHook("vec3"));
        Register(new BuiltinSwizzleHook("vec4"));
        Register(new BuiltinSwizzleHook("vec2<>"));
        Register(new BuiltinSwizzleHook("vec3<>"));
        Register(new BuiltinSwizzleHook("vec4<>"));
    }

    #endregion
}
