using Acorn.Dxil.Data;
using Acorn.Dxil.Encode;
using Acorn.LLVM.Data;
using Acorn.LLVM.Encode;
using Acorn.Spirv.Data;
using Acorn.Spirv.Decode;
using Gnosis.IR.Shader;

namespace Gnosis.Graphic.Shader.Dxil;

/// <summary>
///     SPIR-V → DXIL 转换器，将 SPIR-V 字节码转换为 DXIL/DXContainer 格式。
/// </summary>
/// <remarks>
///     本转换器接收 SPIR-V 字节码，解码后映射为 DXIL 程序结构，
///     最终编码为 DXContainer 二进制格式。
///     遵循架构规则：二进制编解码由 Acorn 独占，Gnosis.Graphic 负责生成逻辑。
/// </remarks>
public sealed class DxilGenerator
{
    #region Fields

    private readonly DxContainerEncoder _containerEncoder = new();
    private readonly DxilProgramEncoder _programEncoder = new();
    private readonly LLVMEncoder _llvmEncoder = new();

    private readonly Dictionary<uint, string> _typeNames = new();
    private readonly Dictionary<uint, DxilShaderModelKind> _executionModelMap = new();

    #endregion

    #region Public Methods

    /// <summary>
    ///     从 SPIR-V 字节码生成 DXContainer 二进制数据。
    /// </summary>
    /// <param name="spirvBytecode">SPIR-V 字节码。</param>
    /// <param name="shaderModel">目标着色器模型类型。</param>
    /// <param name="dxilMajorVersion">DXIL 主版本号（默认 1.0）。</param>
    /// <param name="dxilMinorVersion">DXIL 次版本号（默认 0）。</param>
    /// <returns>DXContainer 二进制数据。</returns>
    public byte[] Generate(byte[] spirvBytecode, DxilShaderModelKind shaderModel,
        byte dxilMajorVersion = 1, byte dxilMinorVersion = 0)
    {
        var decoder = new SpirvDecoder(spirvBytecode);
        var spirvModule = decoder.Decode();

        var llvmBitcode = GenerateBitcode(spirvModule, shaderModel);

        var dxilPartData = _programEncoder.EncodeFromBitcode(
            shaderModel, dxilMajorVersion, dxilMinorVersion, llvmBitcode);

        var featureFlags = ExtractShaderFlags(spirvModule);

        var parts = BuildParts(dxilPartData, featureFlags);

        return _containerEncoder.EncodeParts(parts);
    }

    /// <summary>
    ///     从 ShaderModuleIr 生成 DXContainer 二进制数据。
    /// </summary>
    /// <param name="module">着色器模块 IR。</param>
    /// <param name="shaderModel">目标着色器模型类型。</param>
    /// <param name="dxilMajorVersion">DXIL 主版本号。</param>
    /// <param name="dxilMinorVersion">DXIL 次版本号。</param>
    /// <returns>DXContainer 二进制数据。</returns>
    public byte[] GenerateFromIr(ShaderModuleIr module, DxilShaderModelKind shaderModel,
        byte dxilMajorVersion = 1, byte dxilMinorVersion = 0)
    {
        var spirvGenerator = new Spirv.SpirvGenerator();
        var spirvBytecode = spirvGenerator.Generate(module);

        return Generate(spirvBytecode, shaderModel, dxilMajorVersion, dxilMinorVersion);
    }

    #endregion

    #region Private Methods - Bitcode Generation

    private byte[] GenerateBitcode(SpirvModuleData spirvModule, DxilShaderModelKind shaderModel)
    {
        var entryPoints = spirvModule.Instructions
            .Where(i => i.Opcode == SpirvOpCode.OpEntryPoint)
            .ToList();

        var types = spirvModule.Instructions
            .Where(i => IsTypeInstruction(i.Opcode))
            .ToList();

        var records = new List<LLVMRecordData>();
        var subBlocks = new List<LLVMBlockData>();

        GenerateModuleBlock(spirvModule, shaderModel, records, subBlocks);

        var bitcodeData = new LLVMBitcodeData
        {
            Magic = new LLVMMagicData { Version = 0 },
            TopLevelBlocks =
            [
                new LLVMBlockData
                {
                    BlockID = 8,
                    Name = "MODULE_BLOCK",
                    Records = records,
                    SubBlocks = subBlocks
                }
            ]
        };

        return _llvmEncoder.Encode(bitcodeData);
    }

    private void GenerateModuleBlock(SpirvModuleData spirvModule,
        DxilShaderModelKind shaderModel,
        List<LLVMRecordData> records, List<LLVMBlockData> subBlocks)
    {
        GenerateDxilMetadata(spirvModule, shaderModel, subBlocks);
        GenerateTypeBlock(spirvModule, subBlocks);
    }

    private void GenerateDxilMetadata(SpirvModuleData spirvModule,
        DxilShaderModelKind shaderModel, List<LLVMBlockData> subBlocks)
    {
        var metadataRecords = new List<LLVMRecordData>();

        var shaderModelName = MapShaderModelName(shaderModel);
        metadataRecords.Add(new LLVMRecordData
        {
            Code = 2,
            Name = "dx.shaderModel",
            Operands = [(ulong)shaderModelName.Length, ..shaderModelName.Select(c => (ulong)c), 6, 0]
        });

        subBlocks.Add(new LLVMBlockData
        {
            BlockID = 15,
            Name = "METADATA_BLOCK",
            Records = metadataRecords,
            SubBlocks = []
        });
    }

    private void GenerateTypeBlock(SpirvModuleData spirvModule, List<LLVMBlockData> subBlocks)
    {
        var typeRecords = new List<LLVMRecordData>();

        typeRecords.Add(new LLVMRecordData
        {
            Code = 2,
            Name = "TYPE_VOID",
            Operands = []
        });

        typeRecords.Add(new LLVMRecordData
        {
            Code = 7,
            Name = "TYPE_FLOAT",
            Operands = [32]
        });

        typeRecords.Add(new LLVMRecordData
        {
            Code = 4,
            Name = "TYPE_INT32",
            Operands = [32, 1]
        });

        subBlocks.Add(new LLVMBlockData
        {
            BlockID = 13,
            Name = "TYPE_BLOCK",
            Records = typeRecords,
            SubBlocks = []
        });
    }

    #endregion

    #region Private Methods - Container Assembly

    private List<DxContainerPart> BuildParts(byte[] dxilPartData, DxilShaderFlags flags)
    {
        var parts = new List<DxContainerPart>();

        parts.Add(new DxContainerPart
        {
            Header = new DxContainerPartHeader
            {
                FourCC = (uint)DxilPartFourCC.Dxil,
                Size = (uint)dxilPartData.Length
            },
            Data = dxilPartData
        });

        var featureData = new byte[8];
        var flagsBytes = BitConverter.GetBytes((ulong)flags);
        Array.Copy(flagsBytes, featureData, 8);

        parts.Add(new DxContainerPart
        {
            Header = new DxContainerPartHeader
            {
                FourCC = (uint)DxilPartFourCC.FeatureInfo,
                Size = (uint)featureData.Length
            },
            Data = featureData
        });

        return parts;
    }

    #endregion

    #region Private Methods - Utility

    private static DxilShaderFlags ExtractShaderFlags(SpirvModuleData spirvModule)
    {
        var flags = DxilShaderFlags.None;

        var capabilities = spirvModule.Instructions
            .Where(i => i.Opcode == SpirvOpCode.OpCapability)
            .Select(i => (SpirvCapability)i.Operands[0])
            .ToHashSet();

        if (capabilities.Contains(SpirvCapability.Float64))
        {
            flags |= DxilShaderFlags.UsesDoubles;
        }

        return flags;
    }

    private static string MapShaderModelName(DxilShaderModelKind kind) => kind switch
    {
        DxilShaderModelKind.Vertex => "vs",
        DxilShaderModelKind.Pixel => "ps",
        DxilShaderModelKind.Geometry => "gs",
        DxilShaderModelKind.Hull => "hs",
        DxilShaderModelKind.Domain => "ds",
        DxilShaderModelKind.Compute => "cs",
        DxilShaderModelKind.Library => "lib",
        DxilShaderModelKind.Mesh => "ms",
        DxilShaderModelKind.Amplification => "as",
        DxilShaderModelKind.RayGeneration => "lib",
        DxilShaderModelKind.ClosestHit => "lib",
        DxilShaderModelKind.Miss => "lib",
        DxilShaderModelKind.AnyHit => "lib",
        DxilShaderModelKind.Intersection => "lib",
        DxilShaderModelKind.Callable => "lib",
        _ => "ps"
    };

    private static bool IsTypeInstruction(SpirvOpCode opcode) => opcode switch
    {
        SpirvOpCode.OpTypeVoid => true,
        SpirvOpCode.OpTypeBool => true,
        SpirvOpCode.OpTypeInt => true,
        SpirvOpCode.OpTypeFloat => true,
        SpirvOpCode.OpTypeVector => true,
        SpirvOpCode.OpTypeMatrix => true,
        SpirvOpCode.OpTypeArray => true,
        SpirvOpCode.OpTypeStruct => true,
        SpirvOpCode.OpTypePointer => true,
        SpirvOpCode.OpTypeFunction => true,
        SpirvOpCode.OpTypeImage => true,
        SpirvOpCode.OpTypeSampler => true,
        SpirvOpCode.OpTypeSampledImage => true,
        SpirvOpCode.OpTypeAccelerationStructureKHR => true,
        _ => false
    };

    #endregion
}
