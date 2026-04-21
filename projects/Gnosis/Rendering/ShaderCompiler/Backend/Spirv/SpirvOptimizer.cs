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

    /// <summary>
    /// 对 SPIR-V 二进制执行优化
    /// </summary>
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

        return context.Compact();
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

    #region 死代码消除

    private sealed class DeadCodeEliminationPass : IOptimizationPass
    {
        public string Name => "死代码消除";

        public void Run(SpirvOptContext context)
        {
            var usedIds = new HashSet<uint>();
            var definedIds = new Dictionary<uint, int>();

            #region 第一遍：收集已定义和已使用的 ID

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

            #endregion

            #region 收集入口点相关的输出变量和被存储的值

            var entryPointResultIds = CollectEntryPointResultIds(context.Words);

            #endregion

            #region 标记死代码

            var deadIds = new HashSet<uint>();
            foreach (var kvp in definedIds)
            {
                if (!usedIds.Contains(kvp.Key) && !entryPointResultIds.Contains(kvp.Key))
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

            #endregion
        }

        /// <summary>
        /// 获取指令的结果 ID
        /// </summary>
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

        /// <summary>
        /// 收集指令中引用的其他 ID
        /// </summary>
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

        /// <summary>
        /// 收集所有与入口点结果相关的 ID（输出变量、被存入输出变量的值、返回值）
        /// </summary>
        private static HashSet<uint> CollectEntryPointResultIds(uint[] words)
        {
            var resultIds = new HashSet<uint>();
            var outputVariables = new HashSet<uint>();

            #region 扫描输出变量

            for (int i = 5; i < words.Length;)
            {
                var word0 = words[i];
                var wordCount = (int)(word0 >> 16);
                var opCode = word0 & 0xFFFF;

                if (wordCount == 0) break;

                if (opCode == SpirvConstants.Op.OpVariable && wordCount >= 4)
                {
                    var storageClass = words[i + 3];
                    var resultId = words[i + 2];

                    if (storageClass == SpirvConstants.StorageClass.Output)
                    {
                        outputVariables.Add(resultId);
                        resultIds.Add(resultId);
                    }
                }

                i += wordCount;
            }

            #endregion

            #region 收集存储到输出变量的操作数 ID

            for (int i = 5; i < words.Length;)
            {
                var word0 = words[i];
                var wordCount = (int)(word0 >> 16);
                var opCode = word0 & 0xFFFF;

                if (wordCount == 0) break;

                if (opCode == SpirvConstants.Op.OpStore && wordCount >= 3)
                {
                    var pointerId = words[i + 1];
                    var objectId = words[i + 2];

                    if (outputVariables.Contains(pointerId))
                    {
                        resultIds.Add(objectId);
                    }
                }

                i += wordCount;
            }

            #endregion

            #region 收集入口点函数的返回值 ID

            var entryPointFunctionIds = new HashSet<uint>();

            for (int i = 5; i < words.Length;)
            {
                var word0 = words[i];
                var wordCount = (int)(word0 >> 16);
                var opCode = word0 & 0xFFFF;

                if (wordCount == 0) break;

                if (opCode == SpirvConstants.Op.OpEntryPoint && wordCount >= 4)
                {
                    var functionId = words[i + 2];
                    entryPointFunctionIds.Add(functionId);
                }

                i += wordCount;
            }

            for (int i = 5; i < words.Length;)
            {
                var word0 = words[i];
                var wordCount = (int)(word0 >> 16);
                var opCode = word0 & 0xFFFF;

                if (wordCount == 0) break;

                if (opCode == SpirvConstants.Op.OpReturnValue && wordCount >= 2)
                {
                    var returnValueId = words[i + 1];
                    resultIds.Add(returnValueId);
                }

                i += wordCount;
            }

            #endregion

            return resultIds;
        }
    }

    #endregion

    #region 常量折叠

    private sealed class ConstantFoldingPass : IOptimizationPass
    {
        public string Name => "常量折叠";

        public void Run(SpirvOptContext context)
        {
            var constants = new Dictionary<uint, (uint TypeId, ulong Value)>();
            var typeWidths = new Dictionary<uint, int>();

            #region 第一遍：收集常量定义和类型位宽

            for (int i = 5; i < context.Words.Length;)
            {
                var word0 = context.Words[i];
                var wordCount = (int)(word0 >> 16);
                var opCode = word0 & 0xFFFF;

                if (wordCount == 0) break;

                if (opCode == SpirvConstants.Op.OpTypeInt && wordCount >= 4)
                {
                    var typeId = context.Words[i + 1];
                    var width = (int)context.Words[i + 2];
                    typeWidths[typeId] = width;
                }

                if (opCode == SpirvConstants.Op.OpTypeFloat && wordCount >= 4)
                {
                    var typeId = context.Words[i + 1];
                    var width = (int)context.Words[i + 2];
                    typeWidths[typeId] = width;
                }

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

            #endregion

            #region 第二遍：对算术指令进行常量折叠

            for (int i = 5; i < context.Words.Length;)
            {
                var word0 = context.Words[i];
                var wordCount = (int)(word0 >> 16);
                var opCode = word0 & 0xFFFF;

                if (wordCount == 0) break;

                if (IsFoldableArithmeticOp(opCode) && wordCount >= 5)
                {
                    var resultTypeId = context.Words[i + 1];
                    var resultId = context.Words[i + 2];
                    var operand1Id = context.Words[i + 3];
                    var operand2Id = context.Words[i + 4];

                    var resolvedOp1 = context.ResolveId(operand1Id);
                    var resolvedOp2 = context.ResolveId(operand2Id);

                    if (constants.TryGetValue(resolvedOp1, out var const1)
                        && constants.TryGetValue(resolvedOp2, out var const2)
                        && const1.TypeId == resultTypeId
                        && const2.TypeId == resultTypeId)
                    {
                        var foldedValue = FoldOperation(opCode, const1.Value, const2.Value, resultTypeId, typeWidths);

                        if (foldedValue.HasValue)
                        {
                            var targetConstId = FindOrCreateConstant(
                                context, constants, resultTypeId, foldedValue.Value, typeWidths);

                            context.AddIdAlias(resultId, targetConstId);
                            context.MarkInstructionDead(i, wordCount);
                            constants[resultId] = (resultTypeId, foldedValue.Value);
                        }
                    }
                }

                i += wordCount;
            }

            #endregion

            context.SetConstants(constants);
        }

        /// <summary>
        /// 判断是否为可折叠的算术运算操作码
        /// </summary>
        private static bool IsFoldableArithmeticOp(uint opCode)
        {
            return opCode is SpirvConstants.Op.OpIAdd or SpirvConstants.Op.OpISub
                or SpirvConstants.Op.OpIMul or SpirvConstants.Op.OpSDiv
                or SpirvConstants.Op.OpUDiv or SpirvConstants.Op.OpFAdd
                or SpirvConstants.Op.OpFSub or SpirvConstants.Op.OpFMul
                or SpirvConstants.Op.OpFDiv;
        }

        /// <summary>
        /// 执行常量折叠运算
        /// </summary>
        private static ulong? FoldOperation(
            uint opCode, ulong operand1, ulong operand2, uint typeId, Dictionary<uint, int> typeWidths)
        {
            if (!typeWidths.TryGetValue(typeId, out var bitWidth))
            {
                return null;
            }

            var isFloat = opCode is SpirvConstants.Op.OpFAdd or SpirvConstants.Op.OpFSub
                or SpirvConstants.Op.OpFMul or SpirvConstants.Op.OpFDiv;

            if (isFloat)
            {
                return FoldFloatOperation(opCode, operand1, operand2, bitWidth);
            }

            return opCode switch
            {
                SpirvConstants.Op.OpIAdd => operand1 + operand2,
                SpirvConstants.Op.OpISub => operand1 - operand2,
                SpirvConstants.Op.OpIMul => operand1 * operand2,
                SpirvConstants.Op.OpSDiv => (long)operand2 != 0 ? (ulong)((long)operand1 / (long)operand2) : null,
                SpirvConstants.Op.OpUDiv => operand2 != 0 ? operand1 / operand2 : null,
                _ => null
            };
        }

        /// <summary>
        /// 对浮点常量执行算术运算
        /// </summary>
        private static ulong? FoldFloatOperation(uint opCode, ulong operand1, ulong operand2, int bitWidth)
        {
            if (bitWidth == 32)
            {
                var f1 = BitConverter.UInt32BitsToSingle((uint)operand1);
                var f2 = BitConverter.UInt32BitsToSingle((uint)operand2);

                var result = opCode switch
                {
                    SpirvConstants.Op.OpFAdd => f1 + f2,
                    SpirvConstants.Op.OpFSub => f1 - f2,
                    SpirvConstants.Op.OpFMul => f1 * f2,
                    SpirvConstants.Op.OpFDiv => f2 != 0.0f ? f1 / f2 : null,
                    _ => (float?)null
                };

                if (result.HasValue)
                {
                    return BitConverter.SingleToUInt32Bits(result.Value);
                }

                return null;
            }

            if (bitWidth == 64)
            {
                var d1 = BitConverter.UInt64BitsToDouble(operand1);
                var d2 = BitConverter.UInt64BitsToDouble(operand2);

                var result = opCode switch
                {
                    SpirvConstants.Op.OpFAdd => d1 + d2,
                    SpirvConstants.Op.OpFSub => d1 - d2,
                    SpirvConstants.Op.OpFMul => d1 * d2,
                    SpirvConstants.Op.OpFDiv => d2 != 0.0 ? d1 / d2 : null,
                    _ => (double?)null
                };

                if (result.HasValue)
                {
                    return BitConverter.DoubleToUInt64Bits(result.Value);
                }

                return null;
            }

            return null;
        }

        /// <summary>
        /// 查找已有常量或创建新常量，返回常量的结果 ID
        /// </summary>
        private static uint FindOrCreateConstant(
            SpirvOptContext context,
            Dictionary<uint, (uint TypeId, ulong Value)> constants,
            uint typeId,
            ulong value,
            Dictionary<uint, int> typeWidths)
        {
            foreach (var kvp in constants)
            {
                if (kvp.Value.TypeId == typeId && kvp.Value.Value == value)
                {
                    return kvp.Key;
                }
            }

            var bitWidth = typeWidths.GetValueOrDefault(typeId, 32);
            var newConstId = context.CreateConstant(typeId, value, bitWidth);
            constants[newConstId] = (typeId, value);

            return newConstId;
        }
    }

    #endregion

    #region 公共子表达式消除

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

    #endregion

    #region 循环不变量外提

    private sealed class LoopInvariantCodeMotionPass : IOptimizationPass
    {
        public string Name => "循环不变量外提";

        public void Run(SpirvOptContext context)
        {
            var loops = DetectLoops(context.Words);

            foreach (var loop in loops)
            {
                AnalyzeLoopInvariants(context.Words, loop, context);
            }
        }

        /// <summary>
        /// 检测模块中的所有循环结构
        /// </summary>
        private static List<LoopInfo> DetectLoops(uint[] words)
        {
            var loops = new List<LoopInfo>();

            for (int i = 5; i < words.Length;)
            {
                var word0 = words[i];
                var wordCount = (int)(word0 >> 16);
                var opCode = word0 & 0xFFFF;

                if (wordCount == 0) break;

                if (opCode == SpirvConstants.Op.OpLoopMerge && wordCount >= 4)
                {
                    var mergeBlockId = words[i + 1];
                    var continueTargetId = words[i + 2];

                    var headerIndex = i + wordCount;
                    if (headerIndex < words.Length)
                    {
                        var loop = new LoopInfo
                        {
                            MergeInstructionIndex = i,
                            HeaderInstructionIndex = headerIndex,
                            MergeBlockId = mergeBlockId,
                            ContinueTargetId = continueTargetId
                        };

                        loop.BodyEndIndex = FindLoopBodyEnd(words, headerIndex, mergeBlockId);
                        loops.Add(loop);
                    }
                }

                i += wordCount;
            }

            return loops;
        }

        /// <summary>
        /// 查找循环体结束位置（合并块标签的位置）
        /// </summary>
        private static int FindLoopBodyEnd(uint[] words, int startIndex, uint mergeBlockId)
        {
            for (int i = startIndex; i < words.Length;)
            {
                var word0 = words[i];
                var wordCount = (int)(word0 >> 16);
                var opCode = word0 & 0xFFFF;

                if (wordCount == 0) break;

                if (opCode == SpirvConstants.Op.OpLabel && wordCount >= 2)
                {
                    var labelId = words[i + 1];
                    if (labelId == mergeBlockId)
                    {
                        return i;
                    }
                }

                i += wordCount;
            }

            return words.Length;
        }

        /// <summary>
        /// 分析循环体内的不变量指令
        /// </summary>
        private static void AnalyzeLoopInvariants(uint[] words, LoopInfo loop, SpirvOptContext context)
        {
            var definedInLoop = new HashSet<uint>();
            var invariantCandidates = new List<(int Index, uint ResultId, uint OpCode)>();

            #region 收集循环体内定义的 ID 和算术指令

            for (int i = loop.HeaderInstructionIndex; i < loop.BodyEndIndex;)
            {
                var word0 = words[i];
                var wordCount = (int)(word0 >> 16);
                var opCode = word0 & 0xFFFF;

                if (wordCount == 0) break;

                if (wordCount >= 3 && HasResultId(opCode))
                {
                    var resultId = words[i + 2];
                    definedInLoop.Add(resultId);

                    if (IsArithmeticOrLogicalOp(opCode))
                    {
                        invariantCandidates.Add((i, resultId, opCode));
                    }
                }

                i += wordCount;
            }

            #endregion

            #region 检查每个候选指令的操作数是否都在循环外定义

            foreach (var candidate in invariantCandidates)
            {
                var word0 = words[candidate.Index];
                var wordCount = (int)(word0 >> 16);
                var allOperandsOutside = true;

                for (int opIdx = 3; opIdx < wordCount && allOperandsOutside; opIdx++)
                {
                    var operandId = context.ResolveId(words[candidate.Index + opIdx]);

                    if (definedInLoop.Contains(operandId))
                    {
                        allOperandsOutside = false;
                    }
                }

                if (allOperandsOutside)
                {
                    context.MarkLoopInvariant(candidate.ResultId, candidate.Index);
                }
            }

            #endregion
        }

        /// <summary>
        /// 判断操作码是否产生结果 ID
        /// </summary>
        private static bool HasResultId(uint opCode)
        {
            return opCode is SpirvConstants.Op.OpLoad or SpirvConstants.Op.OpAccessChain
                or SpirvConstants.Op.OpCompositeConstruct or SpirvConstants.Op.OpCompositeExtract
                or SpirvConstants.Op.OpVectorShuffle or SpirvConstants.Op.OpExtInst
                or SpirvConstants.Op.OpSampledImage or SpirvConstants.Op.OpImageSampleImplicitLod
                or SpirvConstants.Op.OpFunctionCall or SpirvConstants.Op.OpConvertFToS
                or SpirvConstants.Op.OpConvertSToF or SpirvConstants.Op.OpIAdd
                or SpirvConstants.Op.OpISub or SpirvConstants.Op.OpIMul
                or SpirvConstants.Op.OpSDiv or SpirvConstants.Op.OpUDiv
                or SpirvConstants.Op.OpFAdd or SpirvConstants.Op.OpFSub
                or SpirvConstants.Op.OpFMul or SpirvConstants.Op.OpFDiv
                or SpirvConstants.Op.OpIEqual or SpirvConstants.Op.OpINotEqual
                or SpirvConstants.Op.OpSLessThan or SpirvConstants.Op.OpSGreaterThan
                or SpirvConstants.Op.OpULessThan or SpirvConstants.Op.OpFOrdEqual
                or SpirvConstants.Op.OpLogicalAnd or SpirvConstants.Op.OpLogicalOr
                or SpirvConstants.Op.OpLogicalNot or SpirvConstants.Op.OpDot;
        }

        /// <summary>
        /// 判断是否为算术或逻辑运算
        /// </summary>
        private static bool IsArithmeticOrLogicalOp(uint opCode)
        {
            return opCode is SpirvConstants.Op.OpIAdd or SpirvConstants.Op.OpISub
                or SpirvConstants.Op.OpIMul or SpirvConstants.Op.OpSDiv
                or SpirvConstants.Op.OpUDiv or SpirvConstants.Op.OpFAdd
                or SpirvConstants.Op.OpFSub or SpirvConstants.Op.OpFMul
                or SpirvConstants.Op.OpFDiv or SpirvConstants.Op.OpIEqual
                or SpirvConstants.Op.OpINotEqual or SpirvConstants.Op.OpSLessThan
                or SpirvConstants.Op.OpSGreaterThan or SpirvConstants.Op.OpULessThan
                or SpirvConstants.Op.OpFOrdEqual or SpirvConstants.Op.OpLogicalAnd
                or SpirvConstants.Op.OpLogicalOr or SpirvConstants.Op.OpLogicalNot
                or SpirvConstants.Op.OpDot;
        }

        /// <summary>
        /// 循环信息数据结构
        /// </summary>
        private class LoopInfo
        {
            public int MergeInstructionIndex { get; set; }
            public int HeaderInstructionIndex { get; set; }
            public int BodyEndIndex { get; set; }
            public uint MergeBlockId { get; set; }
            public uint ContinueTargetId { get; set; }
        }
    }

    #endregion

    #endregion
}

/// <summary>
/// SPIR-V 优化上下文，维护优化过程中的状态
/// </summary>
public sealed class SpirvOptContext
{
    #region Fields

    private readonly HashSet<int> _deadInstructions = new();
    private readonly Dictionary<uint, uint> _idAliases = new();
    private readonly Dictionary<uint, (uint TypeId, ulong Value)> _constants = new();
    private readonly HashSet<uint> _loopInvariantIds = new();
    private readonly List<uint[]> _pendingInstructions = new();
    private uint _nextId;

    #endregion

    #region Constructors

    public SpirvOptContext(uint[] words)
    {
        Words = words;
        _nextId = words.Length >= 4 ? words[3] : 0;
    }

    #endregion

    #region Properties

    public uint[] Words { get; }

    public IReadOnlyDictionary<uint, uint> IdAliases => _idAliases;

    public IReadOnlyDictionary<uint, (uint TypeId, ulong Value)> Constants => _constants;

    #endregion

    #region Public Methods

    /// <summary>
    /// 标记指定位置的指令为死代码
    /// </summary>
    public void MarkInstructionDead(int startIndex, int wordCount)
    {
        for (int i = startIndex; i < startIndex + wordCount && i < Words.Length; i++)
        {
            _deadInstructions.Add(i);
        }
    }

    /// <summary>
    /// 添加 ID 别名映射，将源 ID 重定向到目标 ID
    /// </summary>
    public void AddIdAlias(uint fromId, uint toId)
    {
        _idAliases[fromId] = toId;
    }

    /// <summary>
    /// 设置常量表供后续优化 Pass 使用
    /// </summary>
    public void SetConstants(Dictionary<uint, (uint TypeId, ulong Value)> constants)
    {
        foreach (var kvp in constants)
        {
            _constants[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// 解析 ID，通过别名链找到最终目标 ID
    /// </summary>
    public uint ResolveId(uint id)
    {
        if (_idAliases.TryGetValue(id, out var alias))
        {
            return ResolveId(alias);
        }

        return id;
    }

    /// <summary>
    /// 分配一个新的结果 ID
    /// </summary>
    public uint AllocateId()
    {
        return _nextId++;
    }

    /// <summary>
    /// 创建一个新的 OpConstant 指令并返回其结果 ID
    /// </summary>
    public uint CreateConstant(uint typeId, ulong value, int bitWidth)
    {
        var newId = AllocateId();
        var wordCount = bitWidth <= 32 ? 4 : 5;
        var instruction = new uint[wordCount];

        instruction[0] = ((uint)wordCount << 16) | SpirvConstants.Op.OpConstant;
        instruction[1] = typeId;
        instruction[2] = newId;
        instruction[3] = (uint)(value & 0xFFFFFFFF);

        if (bitWidth > 32)
        {
            instruction[4] = (uint)((value >> 32) & 0xFFFFFFFF);
        }

        _pendingInstructions.Add(instruction);
        _constants[newId] = (typeId, value);

        return newId;
    }

    /// <summary>
    /// 标记一个 ID 为循环不变量
    /// </summary>
    public void MarkLoopInvariant(uint id, int instructionIndex)
    {
        _loopInvariantIds.Add(id);
    }

    /// <summary>
    /// 压缩 SPIR-V 二进制，移除所有标记为死代码的指令并插入新生成的指令
    /// </summary>
    public byte[] Compact()
    {
        var validWords = new List<uint>();

        #region 保留 SPIR-V 头部（5 个字：魔数、版本、生成器魔术数、Bound、Schema）

        for (int i = 0; i < 5 && i < Words.Length; i++)
        {
            validWords.Add(Words[i]);
        }

        #endregion

        #region 插入优化过程中新生成的指令（如常量折叠产生的新常量）

        foreach (var instruction in _pendingInstructions)
        {
            validWords.AddRange(instruction);
        }

        #endregion

        #region 保留未被标记为死亡的原有指令

        for (int i = 5; i < Words.Length; i++)
        {
            if (!_deadInstructions.Contains(i))
            {
                validWords.Add(Words[i]);
            }
        }

        #endregion

        #region 更新 Bound 字段为最新的 ID 上界

        if (validWords.Count > 3)
        {
            validWords[3] = _nextId;
        }

        #endregion

        var result = new byte[validWords.Count * 4];
        Buffer.BlockCopy(validWords.ToArray(), 0, result, 0, result.Length);
        return result;
    }

    #endregion
}
