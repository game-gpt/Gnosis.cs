using System.Numerics;
using Gnosis.Core.StoryCommands;
using Gnosis.Runtime.Interop;
using Gnosis.Runtime.VM;
using Gnosis.Scene.Graph;

namespace Gnosis.Scene.StoryCommands;

public sealed class CharacterCommands : IStoryCharacterCommands
{
    #region 常量

    private const float ScreenWidth = 1920f;
    private const float ScreenHeight = 1080f;

    #endregion

    #region 字段

    private readonly SceneGraph _sceneGraph;
    private readonly Dictionary<string, ISceneNode> _characterNodes = new();

    private static readonly Dictionary<string, Vector2> NamedPositions = new(StringComparer.OrdinalIgnoreCase)
    {
        { "left", new Vector2(ScreenWidth * 0.25f, ScreenHeight * 0.5f) },
        { "center", new Vector2(ScreenWidth * 0.5f, ScreenHeight * 0.5f) },
        { "right", new Vector2(ScreenWidth * 0.75f, ScreenHeight * 0.5f) },
        { "far-left", new Vector2(ScreenWidth * 0.1f, ScreenHeight * 0.5f) },
        { "far-right", new Vector2(ScreenWidth * 0.9f, ScreenHeight * 0.5f) }
    };

    #endregion

    #region 构造函数

    public CharacterCommands(SceneGraph sceneGraph)
    {
        _sceneGraph = sceneGraph;
    }

    #endregion

    #region 角色命令

    /// <summary>
    /// 显示角色立绘
    /// </summary>
    public void ShowCharacter(string name, string position, string? emotion)
    {
        if (name is null)
        {
            return;
        }

        if (!_characterNodes.ContainsKey(name))
        {
            var node = new SceneNode(name);
            node.Position = ResolvePosition(position);
            _sceneGraph.Root.AddChild(node);
            _characterNodes[name] = node;
        }
    }

    /// <summary>
    /// 隐藏角色立绘
    /// </summary>
    public void HideCharacter(string? name)
    {
        if (name is not null)
        {
            if (_characterNodes.ContainsKey(name))
            {
                _sceneGraph.Root.RemoveChild(name);
                _characterNodes.Remove(name);
            }
        }
        else
        {
            foreach (var n in _characterNodes.Keys)
            {
                _sceneGraph.Root.RemoveChild(n);
            }

            _characterNodes.Clear();
        }
    }

    /// <summary>
    /// 移动角色立绘
    /// </summary>
    public void MoveCharacter(string name, string targetPosition, float duration)
    {
        if (name is null)
        {
            return;
        }

        if (_characterNodes.TryGetValue(name, out var node))
        {
            node.Position = ResolvePosition(targetPosition);
        }
    }

    #endregion

    #region VM 绑定方法

    [NativeFunctionBinding(StoryCommandNames.CharacterShow, StoryCommandIds.CharacterShow)]
    public object? StoryCharacterShow(IVMState vm, object?[] args)
    {
        var name = args.ElementAtOrDefault(0)?.ToString();
        var position = args.ElementAtOrDefault(1)?.ToString() ?? "center";
        var expression = args.ElementAtOrDefault(2)?.ToString();

        ShowCharacter(name ?? "", position, expression);
        return null;
    }

    [NativeFunctionBinding(StoryCommandNames.CharacterHide, StoryCommandIds.CharacterHide)]
    public object? StoryCharacterHide(IVMState vm, object?[] args)
    {
        var name = args.ElementAtOrDefault(0)?.ToString();

        HideCharacter(name);
        return null;
    }

    [NativeFunctionBinding(StoryCommandNames.CharacterMove, StoryCommandIds.CharacterMove)]
    public object? StoryCharacterMove(IVMState vm, object?[] args)
    {
        var name = args.ElementAtOrDefault(0)?.ToString();
        var targetPosition = args.ElementAtOrDefault(1)?.ToString() ?? "center";
        var duration = Convert.ToSingle(args.ElementAtOrDefault(2) ?? 0.5);

        MoveCharacter(name ?? "", targetPosition, duration);
        return null;
    }

    #endregion

    #region 私有方法

    private static Vector2 ResolvePosition(string position)
    {
        return NamedPositions.TryGetValue(position, out var pos) ? pos : NamedPositions["center"];
    }

    #endregion
}
