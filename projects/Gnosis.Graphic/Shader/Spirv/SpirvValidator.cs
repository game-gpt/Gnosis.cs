using System.Text;

namespace Gnosis.Graphic.Shader.Spirv;

public sealed class SpirvValidator
{
    #region Fields

    private readonly List<string> _errors = [];
    private readonly List<string> _warnings = [];
    private readonly HashSet<uint> _declaredIds = [];
    private readonly HashSet<uint> _usedIds = [];
    private readonly HashSet<uint> _typeIds = [];
    private readonly HashSet<uint> _functionIds = [];
    private readonly HashSet<uint> _labelIds = [];
    private readonly HashSet<uint> _variableIds = [];
    private readonly Dictionary<uint, uint> _idToType = new();

    #endregion

    #region Public Methods

    public SpirvValidationResult Validate(byte[] spirvBinary)
    {
        _errors.Clear();
        _warnings.Clear();
        _declaredIds.Clear();
        _usedIds.Clear();
        _typeIds.Clear();
        _functionIds.Clear();
        _labelIds.Clear();
        _variableIds.Clear();
        _idToType.Clear();

        if (spirvBinary.Length < 20)
        {
            _errors.Add("SPIR-V 二进制太短，无法包含有效头部");
            return CreateResult();
        }

        if (spirvBinary.Length % 4 != 0)
        {
            _errors.Add("SPIR-V 二进制长度不是 4 的倍数");
            return CreateResult();
        }

        var words = new uint[spirvBinary.Length / 4];
        Buffer.BlockCopy(spirvBinary, 0, words, 0, spirvBinary.Length);

        ValidateHeader(words);

        var index = 5;
        var inFunction = false;
        var inBlock = false;
        var hasCapability = false;
        var hasMemoryModel = false;

        while (index < words.Length)
        {
            var word0 = words[index];
            var wordCount = (int)(word0 >> 16);
            var opCode = word0 & 0xFFFF;

            if (wordCount == 0)
            {
                _errors.Add($"指令 {index}: wordCount 为 0（opCode={opCode}）");
                break;
            }

            if (index + wordCount > words.Length)
            {
                _errors.Add($"指令 {index}: wordCount={wordCount} 超出剩余字数");
                break;
            }

            var operands = new uint[wordCount - 1];
            if (operands.Length > 0)
            {
                Array.Copy(words, index + 1, operands, 0, operands.Length);
            }

            switch (opCode)
            {
                case SpirvConstants.Op.OpCapability:
                    hasCapability = true;
                    break;

                case SpirvConstants.Op.OpMemoryModel:
                    hasMemoryModel = true;
                    break;

                case SpirvConstants.Op.OpTypeVoid:
                case SpirvConstants.Op.OpTypeBool:
                case SpirvConstants.Op.OpTypeInt:
                case SpirvConstants.Op.OpTypeFloat:
                case SpirvConstants.Op.OpTypeVector:
                case SpirvConstants.Op.OpTypeMatrix:
                case SpirvConstants.Op.OpTypeStruct:
                case SpirvConstants.Op.OpTypePointer:
                case SpirvConstants.Op.OpTypeFunction:
                case SpirvConstants.Op.OpTypeImage:
                case SpirvConstants.Op.OpTypeSampler:
                case SpirvConstants.Op.OpTypeSampledImage:
                case SpirvConstants.Op.OpTypeAccelerationStructureKHR:
                    if (operands.Length >= 1)
                    {
                        RegisterId(operands[0], true);
                    }
                    break;

                case SpirvConstants.Op.OpVariable:
                    if (operands.Length >= 2)
                    {
                        RegisterId(operands[1], false);
                        _variableIds.Add(operands[1]);
                    }
                    break;

                case SpirvConstants.Op.OpFunction:
                    if (operands.Length >= 2)
                    {
                        RegisterId(operands[1], false);
                        _functionIds.Add(operands[1]);
                        inFunction = true;
                    }
                    break;

                case SpirvConstants.Op.OpFunctionEnd:
                    inFunction = false;
                    inBlock = false;
                    break;

                case SpirvConstants.Op.OpLabel:
                    if (operands.Length >= 1)
                    {
                        RegisterId(operands[0], false);
                        _labelIds.Add(operands[0]);
                    }
                    inBlock = true;
                    break;

                case SpirvConstants.Op.OpFunctionParameter:
                    if (operands.Length >= 2)
                    {
                        RegisterId(operands[1], false);
                    }
                    break;

                case SpirvConstants.Op.OpLoad:
                    if (operands.Length >= 3)
                    {
                        RegisterId(operands[1], false);
                        TrackUse(operands[2]);
                    }
                    break;

                case SpirvConstants.Op.OpStore:
                    if (operands.Length >= 2)
                    {
                        TrackUse(operands[0]);
                        TrackUse(operands[1]);
                    }
                    break;

                case SpirvConstants.Op.OpAccessChain:
                    if (operands.Length >= 3)
                    {
                        RegisterId(operands[1], false);
                        TrackUse(operands[2]);
                    }
                    break;

                case SpirvConstants.Op.OpCompositeConstruct:
                    if (operands.Length >= 2)
                    {
                        RegisterId(operands[1], false);
                    }
                    break;

                case SpirvConstants.Op.OpCompositeExtract:
                    if (operands.Length >= 2)
                    {
                        RegisterId(operands[1], false);
                    }
                    break;

                case SpirvConstants.Op.OpBranch:
                    if (operands.Length >= 1)
                    {
                        TrackUse(operands[0]);
                    }
                    inBlock = false;
                    break;

                case SpirvConstants.Op.OpReturn:
                case SpirvConstants.Op.OpReturnValue:
                case SpirvConstants.Op.OpKill:
                    inBlock = false;
                    break;
            }

            index += wordCount;
        }

        if (!hasCapability)
        {
            _errors.Add("缺少 OpCapability 指令");
        }

        if (!hasMemoryModel)
        {
            _errors.Add("缺少 OpMemoryModel 指令");
        }

        CheckUndefinedIds();

        return CreateResult();
    }

    #endregion

    #region Private Methods

    private void ValidateHeader(uint[] words)
    {
        var magic = words[0];
        if (magic != SpirvConstants.MagicNumber)
        {
            _errors.Add($"无效的 SPIR-V 魔数: 0x{magic:X8}（期望 0x{SpirvConstants.MagicNumber:X8}）");
        }

        var version = words[1];
        var major = version >> 16;
        var minor = (version >> 8) & 0xFF;

        if (major == 0 || major > 1 || minor > 6)
        {
            _warnings.Add($"不常见的 SPIR-V 版本: {major}.{minor}");
        }

        var bound = words[3];
        if (bound == 0)
        {
            _errors.Add("Bound 为 0");
        }
    }

    private void RegisterId(uint id, bool isType)
    {
        if (id == 0)
        {
            _errors.Add($"声明了 ID 0（无效）");
            return;
        }

        if (_declaredIds.Contains(id))
        {
            _errors.Add($"ID %{id} 被重复声明");
            return;
        }

        _declaredIds.Add(id);

        if (isType)
        {
            _typeIds.Add(id);
        }
    }

    private void TrackUse(uint id)
    {
        _usedIds.Add(id);
    }

    private void CheckUndefinedIds()
    {
        foreach (var id in _usedIds)
        {
            if (!_declaredIds.Contains(id))
            {
                _errors.Add($"使用了未声明的 ID %{id}");
            }
        }
    }

    private SpirvValidationResult CreateResult()
    {
        return new SpirvValidationResult(
            _errors.Count == 0,
            _errors.AsReadOnly(),
            _warnings.AsReadOnly());
    }

    #endregion

    #region Nested Types

    public sealed record SpirvValidationResult(
        bool IsValid,
        IReadOnlyList<string> Errors,
        IReadOnlyList<string> Warnings)
    {
        public override string ToString()
        {
            var sb = new StringBuilder();

            if (IsValid)
            {
                sb.AppendLine("SPIR-V 验证通过");
            }
            else
            {
                sb.AppendLine("SPIR-V 验证失败");
            }

            if (Errors.Count > 0)
            {
                sb.AppendLine("错误:");
                foreach (var error in Errors)
                {
                    sb.AppendLine($"  - {error}");
                }
            }

            if (Warnings.Count > 0)
            {
                sb.AppendLine("警告:");
                foreach (var warning in Warnings)
                {
                    sb.AppendLine($"  - {warning}");
                }
            }

            return sb.ToString();
        }
    }

    #endregion
}
