using Acorn.Spirv.Data;

namespace SpirvCross.Net.Model;

/// <summary>
///     SPIR-V 模块语义模型，从 Acorn.SpirV 解码结果构建的高层表示。
/// </summary>
/// <remarks>
///     本模型是 SpirvCross.Net 的核心数据结构，将 SPIR-V 二进制指令流
///     转换为类型化的语义对象，便于后续翻译器消费。
/// </remarks>
public sealed class SpirvModule
{
    /// <summary>
    ///     SPIR-V 版本号。
    /// </summary>
    public uint Version { get; init; }

    /// <summary>
    ///     生成器魔数。
    /// </summary>
    public uint GeneratorMagic { get; init; }

    /// <summary>
    ///     能力声明列表。
    /// </summary>
    public List<SpirvCapability> Capabilities { get; init; } = [];

    /// <summary>
    ///     扩展列表。
    /// </summary>
    public List<string> Extensions { get; init; } = [];

    /// <summary>
    ///     入口点列表。
    /// </summary>
    public List<SpirvEntryPoint> EntryPoints { get; init; } = [];

    /// <summary>
    ///     类型表（ResultId → SpirvType）。
    /// </summary>
    public Dictionary<uint, SpirvType> Types { get; init; } = [];

    /// <summary>
    ///     全局变量表（ResultId → SpirvVariable）。
    /// </summary>
    public Dictionary<uint, SpirvVariable> GlobalVariables { get; init; } = [];

    /// <summary>
    ///     函数表（ResultId → SpirvFunction）。
    /// </summary>
    public Dictionary<uint, SpirvFunction> Functions { get; init; } = [];

    /// <summary>
    ///     装饰表（TargetId → 装饰列表）。
    /// </summary>
    public Dictionary<uint, List<SpirvDecorationInfo>> Decorations { get; init; } = [];

    /// <summary>
    ///     名称表（TargetId → 名称）。
    /// </summary>
    public Dictionary<uint, string> Names { get; init; } = [];

    /// <summary>
    ///     获取指定 ID 的类型，如果不存在则返回 null。
    /// </summary>
    public SpirvType? GetType(uint resultId) => Types.GetValueOrDefault(resultId);

    /// <summary>
    ///     获取指定 ID 的名称，如果不存在则返回默认名称。
    /// </summary>
    public string GetName(uint resultId) =>
        Names.GetValueOrDefault(resultId, $"_id{resultId}");

    /// <summary>
    ///     获取指定 ID 的装饰列表。
    /// </summary>
    public IReadOnlyList<SpirvDecorationInfo> GetDecorations(uint targetId) =>
        Decorations.GetValueOrDefault(targetId, []);
}
