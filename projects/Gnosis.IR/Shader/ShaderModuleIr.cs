namespace Gnosis.IR.Shader;

public sealed class ShaderModuleIr
{
    #region Constructors

    public ShaderModuleIr() { }

    public ShaderModuleIr(
        string name,
        List<ShaderFunctionIr> functions,
        List<ShaderStructIr> structs,
        List<ShaderGlobalVariableIr> globalVariables,
        List<ShaderEntryPointIr> entryPoints,
        List<ExternalFunctionRef> externalFunctions)
    {
        Name = name;
        Functions = functions;
        Structs = structs;
        GlobalVariables = globalVariables;
        EntryPoints = entryPoints;
        ExternalFunctions = externalFunctions;
    }

    #endregion

    #region Properties

    public string Name { get; set; } = string.Empty;

    public ShaderLanguage Language { get; set; } = ShaderLanguage.GgShader;

    public ShaderTarget Target { get; set; } = ShaderTarget.Spirv;

    public List<ShaderStructIr> Structs { get; } = [];

    public List<ShaderResourceIr> Resources { get; } = [];

    public List<ShaderFunctionIr> Functions { get; } = [];

    public List<ShaderEntryPointIr> EntryPoints { get; } = [];

    public List<ShaderGlobalVariableIr> GlobalVariables { get; } = [];

    public List<TensorInstruction> TensorInstructions { get; } = [];

    public List<ExternalFunctionRef> ExternalFunctions { get; } = [];

    #endregion
}
