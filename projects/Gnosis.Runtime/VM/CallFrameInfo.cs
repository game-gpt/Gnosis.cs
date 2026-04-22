namespace Gnosis.Runtime.VM;

public sealed record CallFrameInfo(int ReturnAddress, int BasePointer, object?[] Locals);
