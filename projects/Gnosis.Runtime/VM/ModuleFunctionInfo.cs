namespace Gnosis.Runtime.VM;

public sealed record ModuleFunctionInfo(string Name, int ParameterCount, int LocalCount, int EntryOffset);
