using Gnosis.Graphic.Sprite2D;
using NUnit.Framework;

namespace Gnosis.Tests.Graphic;

[TestFixture]
public class Transition2DTests
{
    [Test]
    public void Play_SetsIsPlaying_True()
    {
        var transition = new Transition2D(TransitionType.Fade, 1.0f);

        transition.Play();

        Assert.That(transition.IsPlaying, Is.True);
        Assert.That(transition.Progress, Is.EqualTo(0.0f));
        Assert.That(transition.IsReverse, Is.False);
    }

    [Test]
    public void PlayReverse_SetsIsReverse_True()
    {
        var transition = new Transition2D(TransitionType.Fade, 1.0f);

        transition.PlayReverse();

        Assert.That(transition.IsPlaying, Is.True);
        Assert.That(transition.IsReverse, Is.True);
        Assert.That(transition.Progress, Is.EqualTo(1.0f));
    }

    [Test]
    public void Update_AdvancesProgress()
    {
        var transition = new Transition2D(TransitionType.Fade, 2.0f);
        transition.Play();

        transition.Update(1.0f);

        Assert.That(transition.Progress, Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(transition.IsPlaying, Is.True);
    }

    [Test]
    public void Update_CompletesWhenProgressReachesOne()
    {
        var transition = new Transition2D(TransitionType.Fade, 1.0f);
        transition.Play();

        transition.Update(1.0f);

        Assert.That(transition.IsComplete, Is.True);
        Assert.That(transition.IsPlaying, Is.False);
    }

    [Test]
    public void Update_ReverseCompletesWhenProgressReachesZero()
    {
        var transition = new Transition2D(TransitionType.Fade, 1.0f);
        transition.PlayReverse();

        transition.Update(1.0f);

        Assert.That(transition.Progress, Is.EqualTo(0.0f));
        Assert.That(transition.IsPlaying, Is.False);
    }

    [Test]
    public void OnComplete_FiredWhenTransitionFinishes()
    {
        var transition = new Transition2D(TransitionType.Fade, 1.0f);
        var fired = false;
        transition.OnComplete += () => fired = true;
        transition.Play();

        transition.Update(1.0f);

        Assert.That(fired, Is.True);
    }

    [Test]
    public void Stop_SetsIsPlaying_False()
    {
        var transition = new Transition2D(TransitionType.Fade, 1.0f);
        transition.Play();

        transition.Stop();

        Assert.That(transition.IsPlaying, Is.False);
    }

    [Test]
    public void Reset_ResetsProgressAndIsPlaying()
    {
        var transition = new Transition2D(TransitionType.Fade, 1.0f);
        transition.Play();
        transition.Update(0.5f);

        transition.Reset();

        Assert.That(transition.IsPlaying, Is.False);
        Assert.That(transition.Progress, Is.EqualTo(0.0f));
    }

    [Test]
    public void GetFadeColor_ReturnsBlackWithAlphaProgress()
    {
        var transition = new Transition2D(TransitionType.Fade, 1.0f);
        transition.Play();
        transition.Update(0.5f);

        var color = transition.GetFadeColor();

        Assert.That(color.X, Is.EqualTo(0.0f));
        Assert.That(color.Y, Is.EqualTo(0.0f));
        Assert.That(color.Z, Is.EqualTo(0.0f));
        Assert.That(color.W, Is.EqualTo(0.5f).Within(0.01f));
    }

    [Test]
    public void GetSlideOffset_SlideLeft_ReturnsNegativeXOffset()
    {
        var screenSize = new System.Numerics.Vector2(1920, 1080);
        var transition = new Transition2D(TransitionType.SlideLeft, 1.0f);
        transition.Play();
        transition.Update(0.5f);

        var offset = transition.GetSlideOffset(screenSize);

        Assert.That(offset.X, Is.EqualTo(-960.0f).Within(1.0f));
        Assert.That(offset.Y, Is.EqualTo(0.0f));
    }

    [Test]
    public void GetSlideOffset_SlideRight_ReturnsPositiveXOffset()
    {
        var screenSize = new System.Numerics.Vector2(1920, 1080);
        var transition = new Transition2D(TransitionType.SlideRight, 1.0f);
        transition.Play();
        transition.Update(0.5f);

        var offset = transition.GetSlideOffset(screenSize);

        Assert.That(offset.X, Is.EqualTo(960.0f).Within(1.0f));
        Assert.That(offset.Y, Is.EqualTo(0.0f));
    }

    [Test]
    public void GetWipeProgress_WipeLeft_ReturnsEasedProgress()
    {
        var transition = new Transition2D(TransitionType.WipeLeft, 1.0f);
        transition.Play();
        transition.Update(0.5f);

        var progress = transition.GetWipeProgress();

        Assert.That(progress, Is.EqualTo(0.5f).Within(0.01f));
    }

    [Test]
    public void Easing_EaseInQuad_AppliesQuadraticCurve()
    {
        var transition = new Transition2D(TransitionType.Fade, 1.0f);
        transition.Easing = EasingFunction.EaseInQuad;
        transition.Play();
        transition.Update(0.5f);

        var eased = transition.GetEasedProgress();

        Assert.That(eased, Is.EqualTo(0.25f).Within(0.001f));
    }

    [Test]
    public void Easing_EaseOutQuad_AppliesReverseQuadraticCurve()
    {
        var transition = new Transition2D(TransitionType.Fade, 1.0f);
        transition.Easing = EasingFunction.EaseOutQuad;
        transition.Play();
        transition.Update(0.5f);

        var eased = transition.GetEasedProgress();

        Assert.That(eased, Is.EqualTo(0.75f).Within(0.001f));
    }
}
