namespace Gnosis.Runtime.VM;

public sealed record ModuleTypeInfo(string Name, IReadOnlyList<ModuleFieldInfo> Fields);

public sealed record ModuleFieldInfo(string Name, string FieldType);
