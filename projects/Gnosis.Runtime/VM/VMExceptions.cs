namespace Gnosis.Runtime.VM;

/// <summary>
/// 虚拟机异常基类
/// </summary>
public class VMException : Exception
{
    public VMException(string message) : base(message)
    {
    }

    public VMException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// 栈溢出
/// </summary>
public class VMStackOverflowException : VMException
{
    public VMStackOverflowException() : base("虚拟机操作数栈溢出")
    {
    }

    public VMStackOverflowException(string message) : base(message)
    {
    }
}

/// <summary>
/// 栈下溢
/// </summary>
public class VMStackUnderflowException : VMException
{
    public VMStackUnderflowException() : base("虚拟机操作数栈下溢")
    {
    }

    public VMStackUnderflowException(string message) : base(message)
    {
    }
}

/// <summary>
/// 除零
/// </summary>
public class VMDivideByZeroException : VMException
{
    public VMDivideByZeroException() : base("虚拟机除零错误")
    {
    }

    public VMDivideByZeroException(string message) : base(message)
    {
    }
}

/// <summary>
/// 未知操作码
/// </summary>
public class VMUnknownOpCodeException : VMException
{
    public VMUnknownOpCodeException(byte opCode) : base($"虚拟机遇到未知操作码: 0x{opCode:X2}")
    {
    }

    public VMUnknownOpCodeException(string message) : base(message)
    {
    }
}

/// <summary>
/// 重复函数注册
/// </summary>
public class VMDuplicateFunctionException : VMException
{
    public VMDuplicateFunctionException(int id) : base($"原生函数已注册，ID: {id}")
    {
    }

    public VMDuplicateFunctionException(string message) : base(message)
    {
    }
}

/// <summary>
/// 模块未找到
/// </summary>
public class VMModuleNotFoundException : VMException
{
    public VMModuleNotFoundException(string moduleName) : base($"虚拟机模块未找到: {moduleName}")
    {
    }
}

/// <summary>
/// 索引越界
/// </summary>
public class VMIndexOutOfBoundsException : VMException
{
    public VMIndexOutOfBoundsException(int index, int length) : base($"索引 {index} 超出范围，长度: {length}")
    {
    }

    public VMIndexOutOfBoundsException(string message) : base(message)
    {
    }
}

public class VMNullReferenceException : VMException
{
    public VMNullReferenceException(string message) : base($"空引用: {message}")
    {
    }
}

public class VMRuntimeException : VMException
{
    public VMRuntimeException(string message) : base(message)
    {
    }
}
