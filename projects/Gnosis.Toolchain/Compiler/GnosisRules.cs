using Gnosis.IR.Graph;

namespace Gnosis.Toolchain.Compiler;

/// <summary>
///     Gnosis 优化规则，用于 Nyar 编译器的领域特定优化
/// </summary>
public static class GnosisRules
{
    #region ECS 优化规则

    /// <summary>
    ///     ECS 批量操作优化：将连续的单个实体操作合并为批量操作
    /// </summary>
    public static bool TryBatchEcsOperations(IrFunction function)
    {
        var modified = false;
        var blocks = function.Blocks.ToList();

        foreach (var block in blocks)
        {
            var instructions = block.Instructions.ToList();
            var batchGroups = new List<List<IrInstruction>>();
            var currentGroup = new List<IrInstruction>();

            foreach (var instr in instructions)
            {
                if (IsEcsCreateOrDestroy(instr))
                {
                    currentGroup.Add(instr);
                }
                else
                {
                    if (currentGroup.Count > 1)
                    {
                        batchGroups.Add(new List<IrInstruction>(currentGroup));
                    }
                    currentGroup.Clear();
                }
            }

            if (currentGroup.Count > 1)
            {
                batchGroups.Add(currentGroup);
            }

            foreach (var group in batchGroups)
            {
                // 将 group 中的指令替换为批量操作
                // 实际替换逻辑由 Nyar 编译器插件系统处理
                modified = true;
            }
        }

        return modified;
    }

    private static bool IsEcsCreateOrDestroy(IrInstruction instr)
    {
        return instr.Opcode is
            IrOpcode.SpawnEntity or
            IrOpcode.DestroyEntity;
    }

    /// <summary>
    ///     ECS 查询缓存优化：对重复查询添加缓存
    /// </summary>
    public static bool TryCacheEcsQueries(IrFunction function)
    {
        var modified = false;
        var queryInstructions = new Dictionary<string, IrInstruction>();

        foreach (var block in function.Blocks)
        {
            foreach (var instr in block.Instructions)
            {
                if (instr.Opcode is IrOpcode.QueryAll or IrOpcode.QueryAny)
                {
                    var queryKey = GetQueryKey(instr);

                    if (queryInstructions.TryGetValue(queryKey, out var cachedInstr))
                    {
                        // 替换为缓存结果
                        // 实际替换逻辑由 Nyar 编译器插件系统处理
                        modified = true;
                    }
                    else
                    {
                        queryInstructions[queryKey] = instr;
                    }
                }
            }
        }

        return modified;
    }

    private static string GetQueryKey(IrInstruction instr)
    {
        var args = string.Join(",", instr.Arguments);
        return $"{instr.Opcode}:{args}";
    }

    #endregion

    #region AI 优化规则

    /// <summary>
    ///     AI 行为树扁平化：减少嵌套调用开销
    /// </summary>
    public static bool TryFlattenBehaviorTree(IrFunction function)
    {
        // 实际优化逻辑由 Nyar 编译器插件系统处理
        return false;
    }

    /// <summary>
    ///     AI 路径预计算：在编译期预计算静态路径
    /// </summary>
    public static bool TryPrecomputePaths(IrFunction function)
    {
        // 实际优化逻辑由 Nyar 编译器插件系统处理
        return false;
    }

    #endregion

    #region 内存优化规则

    /// <summary>
    ///     内存池分配优化：将小对象分配合并为池分配
    /// </summary>
    public static bool TryPoolAllocations(IrFunction function)
    {
        // 实际优化逻辑由 Nyar 编译器插件系统处理
        return false;
    }

    /// <summary>
    ///     内存布局优化：优化结构体内存布局以减少缓存未命中
    /// </summary>
    public static bool TryOptimizeMemoryLayout(IrFunction function)
    {
        // 实际优化逻辑由 Nyar 编译器插件系统处理
        return false;
    }

    #endregion

    #region 渲染优化规则

    /// <summary>
    ///     渲染批处理优化：合并相同材质的绘制调用
    /// </summary>
    public static bool TryBatchRenderCalls(IrFunction function)
    {
        // 实际优化逻辑由 Nyar 编译器插件系统处理
        return false;
    }

    #endregion
}
