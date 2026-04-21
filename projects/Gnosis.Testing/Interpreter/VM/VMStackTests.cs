using Gnosis.Interpreter.VM;
using NUnit.Framework;

namespace Gnosis.Testing.Interpreter.VM;

/// <summary>
/// 虚拟机栈单元测试
/// </summary>
[TestFixture]
public class VMStackTests : TestBase
{
    #region 操作数栈测试

    [Test]
    public void Push_IncrementsSP()
    {
        var stack = new VMStack();

        stack.Push(42);

        Assert.That(stack.SP, Is.EqualTo(1));
    }

    [Test]
    public void Pop_DecrementsSP()
    {
        var stack = new VMStack();
        stack.Push(42);

        stack.Pop();

        Assert.That(stack.SP, Is.EqualTo(0));
    }

    [Test]
    public void Pop_ReturnsPushedValue()
    {
        var stack = new VMStack();
        stack.Push(42);

        var result = stack.Pop();

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void Peek_DoesNotChangeSP()
    {
        var stack = new VMStack();
        stack.Push(42);

        stack.Peek();

        Assert.That(stack.SP, Is.EqualTo(1));
    }

    [Test]
    public void Peek_ReturnsTopValue()
    {
        var stack = new VMStack();
        stack.Push(10);
        stack.Push(20);

        var result = stack.Peek();

        Assert.That(result, Is.EqualTo(20));
    }

    [Test]
    public void Dup_CopiesTopValue()
    {
        var stack = new VMStack();
        stack.Push(42);

        stack.Dup();

        Assert.That(stack.SP, Is.EqualTo(2));
        Assert.That(stack.Pop(), Is.EqualTo(42));
        Assert.That(stack.Pop(), Is.EqualTo(42));
    }

    [Test]
    public void Pop_OnEmptyStack_ThrowsUnderflow()
    {
        var stack = new VMStack();

        AssertThrows<VMStackUnderflowException>(() => stack.Pop());
    }

    [Test]
    public void Peek_OnEmptyStack_ThrowsUnderflow()
    {
        var stack = new VMStack();

        AssertThrows<VMStackUnderflowException>(() => stack.Peek());
    }

    [Test]
    public void Dup_OnEmptyStack_ThrowsUnderflow()
    {
        var stack = new VMStack();

        AssertThrows<VMStackUnderflowException>(() => stack.Dup());
    }

    [Test]
    public void Push_ExceedsMaxSize_ThrowsOverflow()
    {
        var stack = new VMStack(2);
        stack.Push(1);
        stack.Push(2);

        AssertThrows<VMStackOverflowException>(() => stack.Push(3));
    }

    #endregion

    #region 调用帧测试

    [Test]
    public void PushFrame_PopFrame_RoundTrip()
    {
        var stack = new VMStack();
        stack.PushFrame(100, 0, 3);

        var frame = stack.PopFrame();

        Assert.That(frame.ReturnAddress, Is.EqualTo(100));
        Assert.That(frame.BasePointer, Is.EqualTo(0));
        Assert.That(frame.Locals.Length, Is.EqualTo(3));
    }

    [Test]
    public void PopFrame_OnEmptyFrameStack_ThrowsUnderflow()
    {
        var stack = new VMStack();

        AssertThrows<VMStackUnderflowException>(() => stack.PopFrame());
    }

    [Test]
    public void CurrentFrame_ReturnsLatestFrame()
    {
        var stack = new VMStack();
        stack.PushFrame(10, 0, 1);
        stack.PushFrame(20, 5, 2);

        var frame = stack.CurrentFrame;

        Assert.That(frame, Is.Not.Null);
        Assert.That(frame!.Value.ReturnAddress, Is.EqualTo(20));
        Assert.That(frame.Value.BasePointer, Is.EqualTo(5));
    }

    #endregion

    #region 清空与索引访问测试

    [Test]
    public void Clear_ResetsStack()
    {
        var stack = new VMStack();
        stack.Push(1);
        stack.Push(2);
        stack.PushFrame(10, 0, 1);

        stack.Clear();

        Assert.That(stack.SP, Is.EqualTo(0));
        Assert.That(stack.FrameCount, Is.EqualTo(0));
    }

    [Test]
    public void GetAt_SetAt_WorkCorrectly()
    {
        var stack = new VMStack();
        stack.Push(10);
        stack.Push(20);
        stack.Push(30);

        Assert.That(stack.GetAt(1), Is.EqualTo(20));

        stack.SetAt(1, 99);

        Assert.That(stack.GetAt(1), Is.EqualTo(99));
    }

    #endregion
}
