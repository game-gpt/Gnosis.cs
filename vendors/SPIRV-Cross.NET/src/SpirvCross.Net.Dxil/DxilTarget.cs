using Acorn.Dxil.Data;
using Acorn.Dxil.Encode;
using Acorn.LLVM.Data;
using Acorn.LLVM.Encode;
using Acorn.Spirv.Data;
using SpirvCross.Net.Model;

namespace SpirvCross.Net.Dxil;

/// <summary>
///     SPIR-V → DXIL 目标格式生成器，将 SPIR-V 语义模型翻译为 DXIL/DXContainer 二进制格式。
/// </summary>
/// <remarks>
///     本生成器是 SpirvCross.Net 的 DXIL 后端，
///     依赖 Acorn.Dxil（DXContainer 编解码）和 Acorn.Llvm（LLVM Bitcode 编码）。
///     遵循架构规则：二进制编解码由 Acorn 独占。
/// </remarks>
public sealed class DxilTarget
{
    private readonly DxContainerEncoder _containerEncoder = new();
    private readonly DxilProgramEncoder _programEncoder = new();
    private readonly LLVMEncoder _llvmEncoder = new();

    /// <summary>
    ///     将 SPIR-V 语义模型翻译为 DXContainer 二进制数据。
    /// </summary>
    /// <param name="module">SPIR-V 语义模型。</param>
    /// <param name="shaderModel">目标着色器模型类型。</param>
    /// <param name="dxilMajorVersion">DXIL 主版本号（默认 1）。</param>
    /// <param name="dxilMinorVersion">DXIL 次版本号（默认 0）。</param>
    /// <returns>DXContainer 二进制数据。</returns>
    public byte[] Generate(SpirvModule module, DxilShaderModelKind shaderModel,
        byte dxilMajorVersion = 1, byte dxilMinorVersion = 0)
    {
        var llvmBitcode = GenerateBitcode(module, shaderModel);

        var dxilPartData = _programEncoder.EncodeFromBitcode(
            shaderModel, dxilMajorVersion, dxilMinorVersion, llvmBitcode);

        var flags = ExtractShaderFlags(module);

        var parts = BuildParts(dxilPartData, flags);

        return _containerEncoder.EncodeParts(parts);
    }

    private byte[] GenerateBitcode(SpirvModule module, DxilShaderModelKind shaderModel)
    {
        var records = new List<LLVMRecordData>();
        var subBlocks = new List<LLVMBlockData>();

        GenerateDxilMetadata(module, shaderModel, subBlocks);
        GenerateTypeBlock(module, subBlocks);

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

    private static void GenerateDxilMetadata(SpirvModule module,
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

    private static void GenerateTypeBlock(SpirvModule module, List<LLVMBlockData> subBlocks)
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

    private static List<DxContainerPart> BuildParts(byte[] dxilPartData, DxilShaderFlags flags)
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

    private static DxilShaderFlags ExtractShaderFlags(SpirvModule module)
    {
        var flags = DxilShaderFlags.None;

        if (module.Capabilities.Contains(SpirvCapability.Float64))
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
        _ => "ps"
    };
}
