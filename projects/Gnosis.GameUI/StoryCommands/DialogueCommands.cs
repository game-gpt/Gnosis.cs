using Gnosis.Core.StoryCommands;
using Gnosis.GameUI.Canvas;
using Gnosis.GameUI.Element;
using Gnosis.Runtime.Interop;
using Gnosis.Runtime.VM;

namespace Gnosis.GameUI.StoryCommands;

public sealed class DialogueCommands
{
    #region 字段

    private readonly ICanvas _dialogueCanvas;
    private IUIElement? _dialogueElement;

    #endregion

    #region 构造函数

    public DialogueCommands(ICanvas dialogueCanvas)
    {
        _dialogueCanvas = dialogueCanvas;
    }

    #endregion

    #region 对话命令

    /// <summary>
    /// 显示对话框
    /// Story 语法: %dialogue::show("角色名", "对话内容")
    /// 参数: args[0] = 说话者名称, args[1] = 对话内容
    /// </summary>
    [NativeFunctionBinding(StoryCommandNames.DialogueShow, StoryCommandIds.DialogueShow)]
    public object? StoryDialogueShow(IVMState vm, object?[] args)
    {
        var speaker = args.ElementAtOrDefault(0)?.ToString() ?? "";
        var text = args.ElementAtOrDefault(1)?.ToString() ?? "";

        if (_dialogueElement is not null)
        {
            _dialogueCanvas.RemoveElement(_dialogueElement);
        }

        _dialogueElement = new UIElement
        {
            ElementType = UIElementType.Text,
            R = 1.0f,
            G = 1.0f,
            B = 1.0f,
            A = 1.0f
        };

        _dialogueCanvas.AddElement(_dialogueElement);

        return null;
    }

    /// <summary>
    /// 隐藏对话框
    /// Story 语法: %dialogue::hide()
    /// </summary>
    [NativeFunctionBinding(StoryCommandNames.DialogueHide, StoryCommandIds.DialogueHide)]
    public object? StoryDialogueHide(IVMState vm, object?[] args)
    {
        if (_dialogueElement is not null)
        {
            _dialogueCanvas.RemoveElement(_dialogueElement);
            _dialogueElement = null;
        }

        return null;
    }

    #endregion
}
