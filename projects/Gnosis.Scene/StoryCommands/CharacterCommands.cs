using Gnosis.Core.StoryCommands;
using Gnosis.Runtime.Interop;
using Gnosis.Runtime.VM;
using Gnosis.Scene.Graph;

namespace Gnosis.Scene.StoryCommands;

public sealed class CharacterCommands : IStoryCharacterCommands
{
    #region 字段

    private readonly SceneGraph _sceneGraph;
    private readonly Dictionary<string, ISceneNode> _characterNodes = new();

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
            node.SetPosition(targetPosition);
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
}
