using Gnosis.Core.StoryCommands;
using Gnosis.Runtime.Interop;
using Gnosis.Runtime.VM;
using Gnosis.Scene.Graph;

namespace Gnosis.Scene.StoryCommands;

public sealed class CharacterCommands
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
    /// Story 语法: %character::show("alice", "center", "smile")
    /// 参数: args[0] = 角色名, args[1] = 位置 (可选, 默认 "center"), args[2] = 表情/动画 (可选)
    /// </summary>
    [NativeFunctionBinding(StoryCommandNames.CharacterShow, StoryCommandIds.CharacterShow)]
    public object? StoryCharacterShow(IVMState vm, object?[] args)
    {
        var characterName = args.ElementAtOrDefault(0)?.ToString();

        if (characterName is null)
        {
            return null;
        }

        var position = args.ElementAtOrDefault(1)?.ToString() ?? "center";
        var expression = args.ElementAtOrDefault(2)?.ToString();

        if (!_characterNodes.ContainsKey(characterName))
        {
            var node = new SceneNode(characterName);
            _sceneGraph.Root.AddChild(node);
            _characterNodes[characterName] = node;
        }

        return null;
    }

    /// <summary>
    /// 隐藏角色立绘
    /// Story 语法: %character::hide("alice")
    /// 参数: args[0] = 角色名 (可选, 为空则隐藏所有角色)
    /// </summary>
    [NativeFunctionBinding(StoryCommandNames.CharacterHide, StoryCommandIds.CharacterHide)]
    public object? StoryCharacterHide(IVMState vm, object?[] args)
    {
        var characterName = args.ElementAtOrDefault(0)?.ToString();

        if (characterName is not null)
        {
            if (_characterNodes.ContainsKey(characterName))
            {
                _sceneGraph.Root.RemoveChild(characterName);
                _characterNodes.Remove(characterName);
            }
        }
        else
        {
            foreach (var name in _characterNodes.Keys)
            {
                _sceneGraph.Root.RemoveChild(name);
            }

            _characterNodes.Clear();
        }

        return null;
    }

    /// <summary>
    /// 移动角色立绘
    /// Story 语法: %character::move("alice", "left", 1.0)
    /// 参数: args[0] = 角色名, args[1] = 目标位置, args[2] = 移动时长 (可选, 默认 0.5)
    /// </summary>
    [NativeFunctionBinding(StoryCommandNames.CharacterMove, StoryCommandIds.CharacterMove)]
    public object? StoryCharacterMove(IVMState vm, object?[] args)
    {
        var characterName = args.ElementAtOrDefault(0)?.ToString();

        if (characterName is null)
        {
            return null;
        }

        var targetPosition = args.ElementAtOrDefault(1)?.ToString() ?? "center";
        var duration = Convert.ToSingle(args.ElementAtOrDefault(2) ?? 0.5);

        return null;
    }

    #endregion
}
