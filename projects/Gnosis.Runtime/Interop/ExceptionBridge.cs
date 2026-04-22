using Gnosis.Runtime.VM;

namespace Gnosis.Runtime.Interop;

public static class ExceptionBridge
{
    public static VMException WrapException(Exception ex)
    {
        return ex switch
        {
            VMException vmEx => vmEx,
            NullReferenceException => new VMNullReferenceException(ex.Message),
            IndexOutOfRangeException => new VMIndexOutOfBoundsException(ex.Message),
            DivideByZeroException => new VMDivideByZeroException(ex.Message),
            InvalidOperationException => new VMRuntimeException(ex.Message),
            ArgumentException => new VMRuntimeException($"参数错误: {ex.Message}"),
            _ => new VMRuntimeException($"原生异常: {ex.GetType().Name}: {ex.Message}")
        };
    }

    public static VMException WrapException(string ggMessage)
    {
        return new VMRuntimeException(ggMessage);
    }
}
