namespace Gnosis.Rendering.Backends.ShaderIR;

public sealed record ShaderModuleIr(
    string Name,
    IReadOnlyList<ShaderFunctionIr> Functions,
    IReadOnlyList<ShaderStructIr> Structs,
    IReadOnlyList<ShaderGlobalVariableIr> GlobalVariables,
    IReadOnlyList<ShaderEntryPointIr> EntryPoints,
    IReadOnlyList<ExternalFunctionRef> ExternalFunctions)
{
    public IReadOnlyList<ShaderResourceIr> Resources => GlobalVariables
        .Where(v => v.Resource != null)
        .Select(v => v.Resource!)
        .ToList();
}

public sealed record ShaderGlobalVariableIr(
    string Name,
    ShaderIrType Type,
    StorageClass Storage,
    ShaderResourceIr? Resource = null,
    string? Builtin = null,
    uint? Location = null)
{
    public uint ResultId { get; set; }
}

public sealed record ShaderEntryPointIr(
    string FunctionName,
    ShaderExecutionModel ExecutionModel,
    IReadOnlyList<string> InterfaceVariables);
