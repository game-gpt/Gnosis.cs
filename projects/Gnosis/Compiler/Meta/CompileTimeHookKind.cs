namespace Gnosis.Compiler.Meta;

/// <summary>
/// 编译期钩子类型
/// </summary>
public enum CompileTimeHookKind
{
    /// <summary>
    /// 成员访问钩子：obj.member
    /// </summary>
    MemberAccess,

    /// <summary>
    /// 索引访问钩子：obj[index]
    /// </summary>
    IndexAccess,

    /// <summary>
    /// 调用钩子：obj(args)
    /// </summary>
    Call,

    /// <summary>
    /// 赋值钩子：obj = value
    /// </summary>
    Assignment
}
