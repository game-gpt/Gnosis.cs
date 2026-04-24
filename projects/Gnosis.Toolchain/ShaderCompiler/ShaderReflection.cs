using Gnosis.IR.Shader;

namespace Gnosis.Toolchain.ShaderCompiler;

public sealed class ShaderReflection
{
    #region Properties

    public string Name { get; set; } = string.Empty;

    public List<ShaderEntryPointReflection> EntryPoints { get; } = [];

    public List<ShaderResourceReflection> Resources { get; } = [];

    public List<ShaderStructReflection> Structs { get; } = [];

    public List<ShaderPushConstantReflection> PushConstants { get; } = [];

    #endregion

    #region Public Methods

    public static ShaderReflection FromModule(ShaderModuleIr module)
    {
        var reflection = new ShaderReflection
        {
            Name = module.Name
        };

        foreach (var entryPoint in module.EntryPoints)
        {
            reflection.EntryPoints.Add(new ShaderEntryPointReflection
            {
                Name = entryPoint.Name,
                FunctionName = entryPoint.FunctionName,
                Stage = MapExecutionModelToStage(entryPoint.ExecutionModel)
            });
        }

        foreach (var resource in module.Resources)
        {
            reflection.Resources.Add(new ShaderResourceReflection
            {
                Name = resource.Name,
                Kind = resource.Kind,
                DescriptorSet = resource.DescriptorSet,
                Binding = resource.Binding,
                TypeName = resource.Type.ToString() ?? "unknown"
            });
        }

        foreach (var global in module.GlobalVariables)
        {
            if (global.Resource is null && global.Storage != StorageClass.PushConstant)
            {
                continue;
            }

            if (global.Storage == StorageClass.PushConstant)
            {
                reflection.PushConstants.Add(new ShaderPushConstantReflection
                {
                    Name = global.Name,
                    TypeName = global.Type.ToString() ?? "unknown"
                });
            }
        }

        foreach (var structIr in module.Structs)
        {
            var fields = structIr.Fields.Select(f => new ShaderFieldReflection
            {
                Name = f.Name,
                TypeName = f.Type.ToString() ?? "unknown",
                Offset = f.Offset
            }).ToList();

            reflection.Structs.Add(new ShaderStructReflection
            {
                Name = structIr.Name,
                Size = structIr.Size,
                Fields = fields
            });
        }

        return reflection;
    }

    #endregion

    #region Private Methods

    private static string MapExecutionModelToStage(ShaderExecutionModel model) => model switch
    {
        ShaderExecutionModel.Vertex => "vertex",
        ShaderExecutionModel.Fragment => "fragment",
        ShaderExecutionModel.Geometry => "geometry",
        ShaderExecutionModel.TessellationControl => "tess_ctrl",
        ShaderExecutionModel.TessellationEvaluation => "tess_eval",
        ShaderExecutionModel.GLCompute => "compute",
        ShaderExecutionModel.RayGenerationKHR => "ray_gen",
        ShaderExecutionModel.ClosestHitKHR => "closest_hit",
        ShaderExecutionModel.MissKHR => "miss",
        ShaderExecutionModel.AnyHitKHR => "any_hit",
        ShaderExecutionModel.IntersectionKHR => "intersection",
        ShaderExecutionModel.CallableKHR => "callable",
        ShaderExecutionModel.Mesh => "mesh",
        ShaderExecutionModel.Task => "task",
        _ => "unknown"
    };

    #endregion
}

public sealed class ShaderEntryPointReflection
{
    public string Name { get; set; } = string.Empty;
    public string FunctionName { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
}

public sealed class ShaderResourceReflection
{
    public string Name { get; set; } = string.Empty;
    public ShaderResourceKind Kind { get; set; }
    public uint DescriptorSet { get; set; }
    public uint Binding { get; set; }
    public string TypeName { get; set; } = string.Empty;
}

public sealed class ShaderStructReflection
{
    public string Name { get; set; } = string.Empty;
    public uint Size { get; set; }
    public List<ShaderFieldReflection> Fields { get; } = [];
}

public sealed class ShaderFieldReflection
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public uint Offset { get; set; }
}

public sealed class ShaderPushConstantReflection
{
    public string Name { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
}
