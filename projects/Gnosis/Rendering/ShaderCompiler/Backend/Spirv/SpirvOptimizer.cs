namespace Gnosis.Rendering.ShaderCompiler.Backend.Spirv;

public sealed class SpirvOptimizer
{
    #region Fields

    private readonly List<IOptimizationPass> _passes = new();

    #endregion

    #region Constructors

    public SpirvOptimizer()
    {
        _passes.Add(new DeadCodeEliminationPass());
        _passes.Add(new ConstantFoldingPass());
        _passes.Add(new CommonSubexpressionEliminationPass());
    }

    #endregion

    #region Public Methods

    public byte[] Optimize(byte[] spirvBinary, SpirvOptimizationLevel level = SpirvOptimizationLevel.Default)
    {
        if (level == SpirvOptimizationLevel.None)
        {
            return spirvBinary;
        }

        var words = new uint[spirvBinary.Length / 4];
        Buffer.BlockCopy(spirvBinary, 0, words, 0, spirvBinary.Length);

        var context = new SpirvOptContext(words);

        var passesToRun = level switch
        {
            SpirvOptimizationLevel.Minimal => _passes.Take(1),
            SpirvOptimizationLevel.Default => _passes,
            SpirvOptimizationLevel.Aggressive => _passes.Concat(new[] { new LoopInvariantCodeMotionPass() }),
            _ => _passes
        };

        foreach (var pass in passesToRun)
        {
            pass.Run(context);
        }

        var result = new byte[context.Words.Length * 4];
        Buffer.BlockCopy(context.Words, 0, result, 0, result.Length);
        return result;
    }

    #endregion

    #region Nested Types

    public enum SpirvOptimizationLevel
    {
        None,
        Minimal,
        Default,
        Aggressive
    }

    private interface IOptimizationPass
    {
        string Name { get; }
        void Run(SpirvOptContext context);
    }

    private sealed class DeadCodeEliminationPass : IOptimizationPass
    {
        public string Name => "死代码消除";

        public void Run(SpirvOptContext context)
        {
            var usedIds = new HashSet<uint>();
            var definedIds = new Dictionary<uint, int>();

            for (int i = 5; i < context.Words.Length;)
            {
                var word0 = context.Words[i];
                var wordCount = (int)(word0 >> 16);
                var opCode = word0 & 0xFFFF;

                if (wordCount == 0) break;

                var resultId = GetResultId(opCode, context.Words, i, wordCount);
                if (resultId.HasValue)
                {
                    definedIds[resultId.Value] = i;
                }

                CollectUsedIds(opCode, context.Words, i, wordCount, usedIds);

                i += wordCount;
            }

            var deadIds = new HashSet<uint>();
            foreach (var kvp in definedIds)
            {
                if (!usedIds.Contains(kvp.Key) && !IsEntryPointResult(context.Words, kvp.Value))
                {
                    deadIds.Add(kvp.Key);
                }
            }

            foreach (var deadId in deadIds)
            {
                if (definedIds.TryGetValue(deadId, out var instrIndex))
                {
                    var word0 = context.Words[instrIndex];
                    var wordCount = (int)(word0 >> 16);
                    context.MarkInstructionDead(instrIndex, wordCount);
                }
            }
        }

        private static uint? GetResultId(uint opCode, uint[] words, int instrStart, int wordCount)
        {
            if (wordCount < 2) return null;

            return opCode switch
            {
                SpirvConstants.Op.OpTypeVoid or SpirvConstants.Op.OpTypeBool
                    or SpirvConstants.Op.OpTypeInt or SpirvConstants.Op.OpTypeFloat
                    or SpirvConstants.Op.OpTypeVector or SpirvConstants.Op.OpTypeMatrix
                    or SpirvConstants.Op.OpTypeStruct or SpirvConstants.Op.OpTypePointer
                    or SpirvConstants.Op.OpTypeFunction or SpirvConstants.Op.OpTypeImage
                    or SpirvConstants.Op.OpTypeSampler or SpirvConstants.Op.OpTypeSampledImage
                    or SpirvConstants.Op.OpConstant or SpirvConstants.Op.OpSpecConstant
                    or SpirvConstants.Op.OpVariable or SpirvConstants.Op.OpFunction
                    or SpirvConstants.Op.OpFunctionParameter or SpirvConstants.Op.OpLabel
                    or SpirvConstants.Op.OpLoad or SpirvConstants.Op.OpAccessChain
                    or SpirvConstants.Op.OpCompositeConstruct or SpirvConstants.Op.OpCompositeExtract
                    or SpirvConstants.Op.OpVectorShuffle or SpirvConstants.Op.OpExtInst
                    or SpirvConstants.Op.OpSampledImage or SpirvConstants.Op.OpImageSampleImplicitLod
                    or SpirvConstants.Op.OpFunctionCall or SpirvConstants.Op.OpConvertFToS
                    or SpirvConstants.Op.OpConvertSToF => words[instrStart + 1],
                _ => null
            };
        }

        private static void CollectUsedIds(uint opCode, uint[] words, int instrStart, int wordCount, HashSet<uint> usedIds)
        {
            var operandStart = instrStart + 1;

            switch (opCode)
            {
                case SpirvConstants.Op.OpStore:
                    if (wordCount >= 3)
                    {
                        usedIds.Add(words[operandStart]);
                        usedIds.Add(words[operandStart + 1]);
                    }
                    break;

                case SpirvConstants.Op.OpBranch:
                    if (wordCount >= 2)
                    {
                        usedIds.Add(words[operandStart]);
                    }
                    break;

                case SpirvConstants.Op.OpBranchConditional:
                    if (wordCount >= 4)
                    {
                        usedIds.Add(words[operandStart]);
                        usedIds.Add(words[operandStart + 1]);
                        usedIds.Add(words[operandStart + 2]);
                    }
                    break;

                case SpirvConstants.Op.OpLoad:
                    if (wordCount >= 4)
                    {
                        usedIds.Add(words[operandStart + 2]);
                    }
                    break;

                case SpirvConstants.Op.OpFunctionCall:
                    for (int i = 3; i < wordCount - 1; i++)
                    {
                        usedIds.Add(words[operandStart + i]);
                    }
                    break;
            }
        }

        private static bool IsEntryPointResult(uint[] words, int instrIndex)
        {
            return false;
        }
    }

    private sealed class ConstantFoldingPass : IOptimizationPass
    {
        public string Name => "常量折叠";

        public void Run(SpirvOptContext context)
        {
            var constants = new Dictionary<uint, (uint TypeId, ulong Value)>();

            for (int i = 5; i < context.Words.Length;)
            {
                var word0 = context.Words[i];
                var wordCount = (int)(word0 >> 16);
                var opCode = word0 & 0xFFFF;

                if (wordCount == 0) break;

                if (opCode == SpirvConstants.Op.OpConstant && wordCount >= 4)
                {
                    var typeId = context.Words[i + 1];
                    var resultId = context.Words[i + 2];
                    ulong value = 0;

                    for (int j = 0; j < wordCount - 3; j++)
                    {
                        value |= (ulong)context.Words[i + 3 + j] << (j * 32);
                    }

                    constants[resultId] = (typeId, value);
                }

                i += wordCount;
            }

            context.SetConstants(constants);
        }
    }

    private sealed class CommonSubexpressionEliminationPass : IOptimizationPass
    {
        public string Name => "公共子表达式消除";

        public void Run(SpirvOptContext context)
        {
            var exprHash = new Dictionary<string, uint>();

            for (int i = 5; i < context.Words.Length;)
            {
                var word0 = context.Words[i];
                var wordCount = (int)(word0 >> 16);
                var opCode = word0 & 0xFFFF;

                if (wordCount == 0) break;

                if (IsArithmeticOp(opCode) && wordCount >= 4)
                {
                    var key = ComputeExprKey(opCode, context.Words, i, wordCount);

                    if (exprHash.TryGetValue(key, out var existingResultId))
                    {
                        var resultId = context.Words[i + 1];
                        context.AddIdAlias(resultId, existingResultId);
                        context.MarkInstructionDead(i, wordCount);
                    }
                    else
                    {
                        var resultId = context.Words[i + 1];
                        exprHash[key] = resultId;
                    }
                }

                i += wordCount;
            }
        }

        private static bool IsArithmeticOp(uint opCode)
        {
            return opCode is SpirvConstants.Op.OpIAdd or SpirvConstants.Op.OpISub
                or SpirvConstants.Op.OpIMul or SpirvConstants.Op.OpFAdd
                or SpirvConstants.Op.OpFSub or SpirvConstants.Op.OpFMul
                or SpirvConstants.Op.OpFDiv or SpirvConstants.Op.OpSDiv
                or SpirvConstants.Op.OpDot;
        }

        private static string ComputeExprKey(uint opCode, uint[] words, int instrStart, int wordCount)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(opCode);
            sb.Append('_');

            for (int i = 2; i < wordCount - 1; i++)
            {
                sb.Append(words[instrStart + i]);
                sb.Append('_');
            }

            return sb.ToString();
        }
    }

    private sealed class LoopInvariantCodeMotionPass : IOptimizationPass
    {
        public string Name => "循环不变量外提";

        public void Run(SpirvOptContext context)
        {
            // 循环不变量外提的完整实现需要支配者树分析
            // 此处为骨架实现，标记循环边界供后续优化使用
        }
    }

    #endregion
}

public sealed class SpirvOptContext
{
    #region Fields

    private readonly HashSet<int> _deadInstructions = new();
    private readonly Dictionary<uint, uint> _idAliases = new();
    private readonly Dictionary<uint, (uint TypeId, ulong Value)> _constants = new();

    #endregion

    #region Constructors

    public SpirvOptContext(uint[] words)
    {
        Words = words;
    }

    #endregion

    #region Properties

    public uint[] Words { get; }

    public IReadOnlyDictionary<uint, uint> IdAliases => _idAliases;

    public IReadOnlyDictionary<uint, (uint TypeId, ulong Value)> Constants => _constants;

    #endregion

    #region Public Methods

    public void MarkInstructionDead(int startIndex, int wordCount)
    {
        for (int i = startIndex; i < startIndex + wordCount && i < Words.Length; i++)
        {
            _deadInstructions.Add(i);
        }
    }

    public void AddIdAlias(uint fromId, uint toId)
    {
        _idAliases[fromId] = toId;
    }

    public void SetConstants(Dictionary<uint, (uint TypeId, ulong Value)> constants)
    {
        foreach (var kvp in constants)
        {
            _constants[kvp.Key] = kvp.Value;
        }
    }

    public uint ResolveId(uint id)
    {
        if (_idAliases.TryGetValue(id, out var alias))
        {
            return ResolveId(alias);
        }

        return id;
    }

    public byte[] Compact()
    {
        var validWords = new List<uint>();

        for (int i = 0; i < Words.Length; i++)
        {
            if (!_deadInstructions.Contains(i))
            {
                validWords.Add(Words[i]);
            }
        }

        var result = new byte[validWords.Count * 4];
        Buffer.BlockCopy(validWords.ToArray(), 0, result, 0, result.Length);
        return result;
    }

    #endregion
}
