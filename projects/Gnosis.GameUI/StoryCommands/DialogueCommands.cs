using Gnosis.Core.StoryCommands;
using Gnosis.GameUI.Canvas;
using Gnosis.GameUI.Element;
using Gnosis.Runtime.Interop;
using Gnosis.Runtime.VM;

namespace Gnosis.GameUI.StoryCommands;

public sealed class DialogueCommands : IStoryDialogueCommands
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
    /// </summary>
    public void ShowDialogue(string? speaker, string text, string? emotion)
    {
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
    }

    /// <summary>
    /// 隐藏对话框
    /// </summary>
    public void HideDialogue()
    {
        if (_dialogueElement is not null)
        {
            _dialogueCanvas.RemoveElement(_dialogueElement);
            _dialogueElement = null;
        }
    }

    #endregion

    #region VM 绑定方法

    [NativeFunctionBinding(StoryCommandNames.DialogueShow, StoryCommandIds.DialogueShow)]
    public object? StoryDialogueShow(IVMState vm, object?[] args)
    {
        var speaker = args.ElementAtOrDefault(0)?.ToString() ?? "";
        var text = args.ElementAtOrDefault(1)?.ToString() ?? "";
        var emotion = args.ElementAtOrDefault(2)?.ToString();

        ShowDialogue(speaker, text, emotion);
        return null;
    }

    [NativeFunctionBinding(StoryCommandNames.DialogueHide, StoryCommandIds.DialogueHide)]
    public object? StoryDialogueHide(IVMState vm, object?[] args)
    {
        HideDialogue();
        return null;
    }

    #endregion
}
