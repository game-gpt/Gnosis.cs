using Gnosis.Core.StoryCommands;
using Gnosis.Graphic.Pipeline;
using Gnosis.Runtime.Interop;
using Gnosis.Runtime.VM;

namespace Gnosis.Graphic.StoryCommands;

public sealed class EffectCommands : IStoryEffectCommands
{
    #region 字段

    private readonly IRenderPipeline _pipeline;
    private readonly List<IRenderPass> _activeEffectPasses = new();

    #endregion

    #region 构造函数

    public EffectCommands(IRenderPipeline pipeline)
    {
        _pipeline = pipeline;
    }

    #endregion

    #region Effect 命令

    /// <summary>
    /// 闪光特效
    /// </summary>
    public void Flash(float r, float g, float b, float duration)
    {
        var flashPass = new RenderPass("StoryFlash", (context, cmd) =>
        {
            context.SetClearColor(r, g, b);
            context.BlendMode = BlendMode.Additive;
        });

        _pipeline.AddPass(flashPass);
        _activeEffectPasses.Add(flashPass);
    }

    /// <summary>
    /// 震动特效
    /// </summary>
    public void Shake(float duration, float intensity)
    {
        var shakePass = new RenderPass("StoryShake", (context, cmd) =>
        {
            var offsetX = (Random.Shared.NextSingle() - 0.5f) * intensity;
            var offsetY = (Random.Shared.NextSingle() - 0.5f) * intensity;
            context.SetViewOffset(offsetX, offsetY);
        });

        _pipeline.AddPass(shakePass);
        _activeEffectPasses.Add(shakePass);
    }

    /// <summary>
    /// 淡出特效
    /// </summary>
    public void FadeOut(float duration, float r, float g, float b)
    {
        var fadePass = new RenderPass("StoryFadeOut", (context, cmd) =>
        {
            context.SetClearColor(r, g, b);
            context.BlendMode = BlendMode.Alpha;
        });

        _pipeline.AddPass(fadePass);
        _activeEffectPasses.Add(fadePass);
    }

    /// <summary>
    /// 闪电特效
    /// </summary>
    public void Lightning(float duration)
    {
        var lightningPass = new RenderPass("StoryLightning", (context, cmd) =>
        {
            context.SetClearColor(1.0f, 1.0f, 1.0f);
            context.BlendMode = BlendMode.Additive;
        });

        _pipeline.AddPass(lightningPass);
        _activeEffectPasses.Add(lightningPass);
    }

    #endregion

    #region VM 绑定方法

    [NativeFunctionBinding(StoryCommandNames.EffectFlash, StoryCommandIds.EffectFlash)]
    public object? StoryEffectFlash(IVMState vm, object?[] args)
    {
        var r = Convert.ToSingle(args.ElementAtOrDefault(0) ?? 1.0);
        var g = Convert.ToSingle(args.ElementAtOrDefault(1) ?? 1.0);
        var b = Convert.ToSingle(args.ElementAtOrDefault(2) ?? 1.0);
        var duration = Convert.ToSingle(args.ElementAtOrDefault(3) ?? 0.5);

        Flash(r, g, b, duration);
        return null;
    }

    [NativeFunctionBinding(StoryCommandNames.EffectShake, StoryCommandIds.EffectShake)]
    public object? StoryEffectShake(IVMState vm, object?[] args)
    {
        var duration = Convert.ToSingle(args.ElementAtOrDefault(0) ?? 0.5);
        var intensity = Convert.ToSingle(args.ElementAtOrDefault(1) ?? 10.0);

        Shake(duration, intensity);
        return null;
    }

    [NativeFunctionBinding(StoryCommandNames.EffectFadeOut, StoryCommandIds.EffectFadeOut)]
    public object? StoryEffectFadeOut(IVMState vm, object?[] args)
    {
        var duration = Convert.ToSingle(args.ElementAtOrDefault(0) ?? 1.0);
        var r = Convert.ToSingle(args.ElementAtOrDefault(1) ?? 0.0);
        var g = Convert.ToSingle(args.ElementAtOrDefault(2) ?? 0.0);
        var b = Convert.ToSingle(args.ElementAtOrDefault(3) ?? 0.0);

        FadeOut(duration, r, g, b);
        return null;
    }

    [NativeFunctionBinding(StoryCommandNames.EffectLightning, StoryCommandIds.EffectLightning)]
    public object? StoryEffectLightning(IVMState vm, object?[] args)
    {
        var duration = Convert.ToSingle(args.ElementAtOrDefault(0) ?? 0.3);

        Lightning(duration);
        return null;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 清理所有特效 Pass
    /// </summary>
    public void Cleanup()
    {
        foreach (var pass in _activeEffectPasses)
        {
            _pipeline.RemovePass(pass.Name);
        }

        _activeEffectPasses.Clear();
    }

    #endregion
}
