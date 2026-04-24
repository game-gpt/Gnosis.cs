using Gnosis.Core.StoryCommands;
using Gnosis.Runtime.Interop;
using Gnosis.Runtime.VM;

namespace Gnosis.Runtime.Verse;

/// <summary>
/// Verse 原生命令桥接，将 Verse IR 中的 CallNative 指令桥接到 Story 命令系统
/// </summary>
public sealed class VerseNativeBridge
{
    #region 字段

    private readonly NativeFunctionRegistry _registry;
    private readonly VerseStoryRuntime _runtime;
    private readonly Dictionary<string, int> _nameToId = new(StringComparer.Ordinal);
    private int _nextCustomId = 2000;

    #endregion

    #region 构造函数

    public VerseNativeBridge(NativeFunctionRegistry registry, VerseStoryRuntime runtime)
    {
        _registry = registry;
        _runtime = runtime;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 注册所有 Verse 原生命令到 VM 原生函数注册表
    /// </summary>
    public void RegisterAll()
    {
        RegisterStoryCommands();
        RegisterVerseRuntimeCommands();
    }

    /// <summary>
    /// 注销所有 Verse 原生命令
    /// </summary>
    public void UnregisterAll()
    {
        foreach (var (_, id) in _nameToId)
        {
            _registry.Unregister(id);
        }

        _nameToId.Clear();
    }

    #endregion

    #region Story 命令注册

    private void RegisterStoryCommands()
    {
        RegisterNativeFunction(StoryCommandIds.DialogueShow, StoryCommandNames.DialogueShow, 3,
            (vm, args) =>
            {
                var speaker = args.ElementAtOrDefault(0).ToStringValue();
                var text = args.ElementAtOrDefault(1).ToStringValue();
                var emotion = args.ElementAtOrDefault(2).ToStringValue();
                _runtime.ShowDialogue(speaker, text, emotion);
                return GGValue.Null;
            });

        RegisterNativeFunction(StoryCommandIds.DialogueHide, StoryCommandNames.DialogueHide, 0,
            (vm, args) => GGValue.Null);

        RegisterNativeFunction(StoryCommandIds.SceneChange, StoryCommandNames.SceneChange, 2,
            (vm, args) =>
            {
                var bgName = args.ElementAtOrDefault(0).ToStringValue();
                var transition = args.ElementAtOrDefault(1).ToStringValue();
                return GGValue.Null;
            });

        RegisterNativeFunction(StoryCommandIds.SceneTransition, StoryCommandNames.SceneTransition, 2,
            (vm, args) =>
            {
                var type = args.ElementAtOrDefault(0).ToStringValue();
                return GGValue.Null;
            });

        RegisterNativeFunction(StoryCommandIds.CharacterShow, StoryCommandNames.CharacterShow, 3,
            (vm, args) =>
            {
                var name = args.ElementAtOrDefault(0).ToStringValue();
                var position = args.ElementAtOrDefault(1).ToStringValue();
                var emotion = args.ElementAtOrDefault(2).ToStringValue();
                return GGValue.Null;
            });

        RegisterNativeFunction(StoryCommandIds.CharacterHide, StoryCommandNames.CharacterHide, 1,
            (vm, args) =>
            {
                var name = args.ElementAtOrDefault(0).ToStringValue();
                return GGValue.Null;
            });

        RegisterNativeFunction(StoryCommandIds.CharacterMove, StoryCommandNames.CharacterMove, 3,
            (vm, args) =>
            {
                var name = args.ElementAtOrDefault(0).ToStringValue();
                return GGValue.Null;
            });

        RegisterNativeFunction(StoryCommandIds.EffectFlash, StoryCommandNames.EffectFlash, 4,
            (vm, args) => GGValue.Null);

        RegisterNativeFunction(StoryCommandIds.EffectShake, StoryCommandNames.EffectShake, 2,
            (vm, args) => GGValue.Null);

        RegisterNativeFunction(StoryCommandIds.EffectFadeOut, StoryCommandNames.EffectFadeOut, 4,
            (vm, args) => GGValue.Null);

        RegisterNativeFunction(StoryCommandIds.EffectLightning, StoryCommandNames.EffectLightning, 1,
            (vm, args) => GGValue.Null);

        RegisterNativeFunction(StoryCommandIds.AudioPlay, StoryCommandNames.AudioPlay, 2,
            (vm, args) =>
            {
                var fileName = args.ElementAtOrDefault(0).ToStringValue();
                return GGValue.Null;
            });

        RegisterNativeFunction(StoryCommandIds.AudioStop, StoryCommandNames.AudioStop, 0,
            (vm, args) => GGValue.Null);

        RegisterNativeFunction(StoryCommandIds.AudioPlaySe, StoryCommandNames.AudioPlaySe, 2,
            (vm, args) =>
            {
                var fileName = args.ElementAtOrDefault(0).ToStringValue();
                return GGValue.Null;
            });

        RegisterNativeFunction(StoryCommandIds.AudioFadeOut, StoryCommandNames.AudioFadeOut, 1,
            (vm, args) => GGValue.Null);

        RegisterNativeFunction(StoryCommandIds.QuestStart, StoryCommandNames.QuestStart, 2,
            (vm, args) =>
            {
                var questId = args.ElementAtOrDefault(0).ToStringValue();
                return GGValue.Null;
            });

        RegisterNativeFunction(StoryCommandIds.QuestComplete, StoryCommandNames.QuestComplete, 1,
            (vm, args) =>
            {
                var questId = args.ElementAtOrDefault(0).ToStringValue();
                return GGValue.Null;
            });

        RegisterNativeFunction(StoryCommandIds.QuestUpdate, StoryCommandNames.QuestUpdate, 2,
            (vm, args) =>
            {
                var questId = args.ElementAtOrDefault(0).ToStringValue();
                return GGValue.Null;
            });
    }

    #endregion

    #region Verse 运行时命令注册

    private void RegisterVerseRuntimeCommands()
    {
        var versePauseId = AllocateId("verse_pause");
        RegisterNativeFunction(versePauseId, "verse_pause", 1,
            (vm, args) =>
            {
                var duration = args.ElementAtOrDefault(0).ToFloat64();
                _runtime.Pause(duration);
                return GGValue.Null;
            });

        var verseWaitId = AllocateId("verse_wait");
        RegisterNativeFunction(verseWaitId, "verse_wait", 1,
            (vm, args) =>
            {
                var duration = args.ElementAtOrDefault(0).ToFloat64();
                _runtime.Wait(duration);
                return GGValue.Null;
            });

        var verseMenuChoiceId = AllocateId("verse_menu_choice");
        RegisterNativeFunction(verseMenuChoiceId, "verse_menu_choice", 1,
            (vm, args) =>
            {
                var text = args.ElementAtOrDefault(0).ToStringValue();
                return GGValue.FromInt(0);
            });

        RegisterPushHelpers();
    }

    private void RegisterPushHelpers()
    {
        var pushStringId = AllocateId("push_string");
        RegisterNativeFunction(pushStringId, "push_string", 1,
            (vm, args) =>
            {
                var hash = args.ElementAtOrDefault(0).ToInt32();
                return GGValue.FromInt(hash);
            });

        var pushI32Id = AllocateId("push_i32");
        RegisterNativeFunction(pushI32Id, "push_i32", 1,
            (vm, args) => args.ElementAtOrDefault(0));

        var pushF32Id = AllocateId("push_f32");
        RegisterNativeFunction(pushF32Id, "push_f32", 1,
            (vm, args) => args.ElementAtOrDefault(0));

        var pushF64Id = AllocateId("push_f64");
        RegisterNativeFunction(pushF64Id, "push_f64", 1,
            (vm, args) => args.ElementAtOrDefault(0));

        var pushBoolId = AllocateId("push_bool");
        RegisterNativeFunction(pushBoolId, "push_bool", 1,
            (vm, args) => args.ElementAtOrDefault(0));

        var pushNullId = AllocateId("push_null");
        RegisterNativeFunction(pushNullId, "push_null", 0,
            (vm, args) => GGValue.Null);
    }

    #endregion

    #region 辅助方法

    private void RegisterNativeFunction(int id, string name, int paramCount, Func<IVMState, GGValue[], GGValue> implementation)
    {
        _nameToId[name] = id;
        _registry.Register(new DelegateNativeFunction(id, name, paramCount, implementation));
    }

    private int AllocateId(string name)
    {
        var id = _nextCustomId++;
        _nameToId[name] = id;
        return id;
    }

    #endregion
}

/// <summary>
/// 基于 Delegate 的原生函数实现
/// </summary>
internal sealed class DelegateNativeFunction : INativeFunction
{
    private readonly Func<IVMState, GGValue[], GGValue> _implementation;

    public int Id { get; }
    public string Name { get; }
    public int ParameterCount { get; }

    public DelegateNativeFunction(int id, string name, int paramCount, Func<IVMState, GGValue[], GGValue> implementation)
    {
        Id = id;
        Name = name;
        ParameterCount = paramCount;
        _implementation = implementation;
    }

    public GGValue Execute(IVMState vm, GGValue[] args)
    {
        return _implementation(vm, args);
    }
}

/// <summary>
/// GGValue 扩展方法
/// </summary>
internal static class GGValueVerseExtensions
{
    public static string ToStringValue(this GGValue value)
    {
        if (value.IsNull)
        {
            return string.Empty;
        }

        if (value.IsReference && value.Reference is string str)
        {
            return str;
        }

        return value.ToString() ?? string.Empty;
    }

    public static double ToFloat64(this GGValue value)
    {
        if (value.IsFloat)
        {
            return value.FloatValue;
        }

        if (value.IsInt)
        {
            return value.IntValue;
        }

        return 0.0;
    }

    public static int ToInt32(this GGValue value)
    {
        if (value.IsInt)
        {
            return (int)value.IntValue;
        }

        if (value.IsFloat)
        {
            return (int)value.FloatValue;
        }

        return 0;
    }
}
