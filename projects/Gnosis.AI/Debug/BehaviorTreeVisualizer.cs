using System.Text;
using Gnosis.AI.Behavior;
using Gnosis.AI.Blackboard;

namespace Gnosis.AI.Debug;

/// <summary>
/// 行为树可视化器，将行为树结构和执行状态渲染为文本树形图。
/// 支持节点状态高亮、执行路径追踪和参数实时查看。
/// </summary>
public sealed class BehaviorTreeVisualizer
{
    #region 内部类型

    /// <summary>
    /// 可视化配置
    /// </summary>
    public struct VisualizerConfig
    {
        /// <summary>
        /// 缩进字符串
        /// </summary>
        public string Indent { get; set; }

        /// <summary>
        /// 分支连接符
        /// </summary>
        public string Branch { get; set; }

        /// <summary>
        /// 末尾分支连接符
        /// </summary>
        public string LastBranch { get; set; }

        /// <summary>
        /// 垂直连接线
        /// </summary>
        public string VerticalLine { get; set; }

        /// <summary>
        /// 是否显示节点状态
        /// </summary>
        public bool ShowStatus { get; set; }

        /// <summary>
        /// 是否高亮活跃路径
        /// </summary>
        public bool HighlightActivePath { get; set; }

        /// <summary>
        /// 是否显示节点类型
        /// </summary>
        public bool ShowNodeType { get; set; }

        /// <summary>
        /// 是否显示子节点计数
        /// </summary>
        public bool ShowChildCount { get; set; }

        /// <summary>
        /// 活跃节点标记
        /// </summary>
        public string ActiveMarker { get; set; }

        /// <summary>
        /// 成功状态标记
        /// </summary>
        public string SuccessMarker { get; set; }

        /// <summary>
        /// 失败状态标记
        /// </summary>
        public string FailureMarker { get; set; }

        /// <summary>
        /// 运行中状态标记
        /// </summary>
        public string RunningMarker { get; set; }

        /// <summary>
        /// 获取默认配置
        /// </summary>
        public static VisualizerConfig Default => new()
        {
            Indent = "  ",
            Branch = "├─",
            LastBranch = "└─",
            VerticalLine = "│ ",
            ShowStatus = true,
            HighlightActivePath = true,
            ShowNodeType = true,
            ShowChildCount = true,
            ActiveMarker = "▶",
            SuccessMarker = "✔",
            FailureMarker = "✘",
            RunningMarker = "⟳"
        };
    }

    #endregion

    #region 字段

    private readonly BehaviorTreeDebugger? _debugger;
    private VisualizerConfig _config;

    #endregion

    #region 属性

    /// <summary>
    /// 可视化配置
    /// </summary>
    public VisualizerConfig Config
    {
        get => _config;
        set => _config = value;
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 创建行为树可视化器
    /// </summary>
    /// <param name="debugger">调试器（可选，提供执行状态信息）</param>
    /// <param name="config">可视化配置</param>
    public BehaviorTreeVisualizer(BehaviorTreeDebugger? debugger = null, VisualizerConfig? config = null)
    {
        _debugger = debugger;
        _config = config ?? VisualizerConfig.Default;
    }

    #endregion

    #region 公开方法 - 行为树结构可视化

    /// <summary>
    /// 将行为树渲染为文本树形图
    /// </summary>
    /// <param name="tree">行为树</param>
    /// <returns>文本树形图</returns>
    public string RenderTree(IBehaviorTree tree)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"行为树: {tree.Name}");

        if (tree.Root is null)
        {
            sb.AppendLine("  (空树)");
            return sb.ToString();
        }

        var latestSnapshots = _debugger?.GetAllLatestSnapshots();
        RenderNode(tree.Root, sb, "", true, latestSnapshots);

        return sb.ToString();
    }

    /// <summary>
    /// 将行为树当前执行状态渲染为文本树形图（带状态高亮）
    /// </summary>
    /// <param name="debugger">调试器</param>
    /// <returns>带状态高亮的文本树形图</returns>
    public string RenderTreeWithStatus(BehaviorTreeDebugger debugger)
    {
        var tree = debugger.Tree;
        var sb = new StringBuilder();

        var latestRecord = debugger.LatestRecord;
        var frameInfo = latestRecord is not null
            ? $" (帧 {latestRecord.FrameNumber}, 耗时 {latestRecord.TotalExecutionTimeMs:F2}ms)"
            : "";

        sb.AppendLine($"行为树: {tree.Name}{frameInfo}");

        if (tree.Root is null)
        {
            sb.AppendLine("  (空树)");
            return sb.ToString();
        }

        var latestSnapshots = debugger.GetAllLatestSnapshots();
        var activePath = debugger.GetCurrentActivePath();
        var activeNames = new HashSet<string>();

        foreach (var snap in activePath)
        {
            activeNames.Add(snap.Name);
        }

        RenderNode(tree.Root, sb, "", true, latestSnapshots, activeNames);

        return sb.ToString();
    }

    #endregion

    #region 公开方法 - 执行路径可视化

    /// <summary>
    /// 渲染当前执行路径
    /// </summary>
    /// <param name="debugger">调试器</param>
    /// <returns>执行路径文本</returns>
    public string RenderActivePath(BehaviorTreeDebugger debugger)
    {
        var activePath = debugger.GetCurrentActivePath();

        if (activePath.Count == 0)
        {
            return "无活跃执行路径";
        }

        var sb = new StringBuilder();
        sb.AppendLine("执行路径:");

        for (var i = 0; i < activePath.Count; i++)
        {
            var snap = activePath[i];
            var indent = new string(' ', i * 2);
            var marker = GetStatusMarker(snap.Status);
            var typeInfo = _config.ShowNodeType ? $" [{snap.NodeType}]" : "";
            sb.AppendLine($"{indent}{marker} {snap.Name}{typeInfo}");
        }

        return sb.ToString();
    }

    #endregion

    #region 公开方法 - 节点参数可视化

    /// <summary>
    /// 渲染节点详细参数信息
    /// </summary>
    /// <param name="debugger">调试器</param>
    /// <param name="nodeName">节点名称</param>
    /// <returns>节点参数文本</returns>
    public string RenderNodeDetails(BehaviorTreeDebugger debugger, string nodeName)
    {
        var snapshot = debugger.GetLatestNodeSnapshot(nodeName);

        if (snapshot is null)
        {
            return $"节点 '{nodeName}' 未找到";
        }

        var sb = new StringBuilder();
        sb.AppendLine($"节点: {snapshot.Value.Name}");
        sb.AppendLine($"  类型: {snapshot.Value.NodeType}");
        sb.AppendLine($"  状态: {snapshot.Value.Status}");
        sb.AppendLine($"  深度: {snapshot.Value.Depth}");
        sb.AppendLine($"  子节点数: {snapshot.Value.ChildCount}");
        sb.AppendLine($"  活跃: {snapshot.Value.IsActive}");
        sb.AppendLine($"  帧号: {snapshot.Value.FrameNumber}");

        if (snapshot.Value.ExecutionTimeMs > 0)
        {
            sb.AppendLine($"  耗时: {snapshot.Value.ExecutionTimeMs:F3}ms");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 渲染黑板内容
    /// </summary>
    /// <param name="blackboard">黑板</param>
    /// <returns>黑板内容文本</returns>
    public string RenderBlackboard(IBlackboard blackboard)
    {
        var sb = new StringBuilder();
        sb.AppendLine("黑板:");

        var keys = new List<string>();

        if (blackboard is Blackboard.Blackboard concreteBlackboard)
        {
            keys = GetBlackboardKeys(concreteBlackboard);
        }

        if (keys.Count == 0)
        {
            sb.AppendLine("  (空)");
            return sb.ToString();
        }

        foreach (var key in keys)
        {
            var value = blackboard.GetValue<object>(key);
            var typeInfo = value?.GetType().Name ?? "null";
            sb.AppendLine($"  {key} = {value} ({typeInfo})");
        }

        return sb.ToString();
    }

    #endregion

    #region 公开方法 - 历史可视化

    /// <summary>
    /// 渲染最近 N 次状态变化摘要
    /// </summary>
    /// <param name="debugger">调试器</param>
    /// <param name="count">记录数</param>
    /// <returns>状态变化摘要文本</returns>
    public string RenderRecentHistory(BehaviorTreeDebugger debugger, int count = 10)
    {
        var records = debugger.GetRecentRecords(count);

        if (records.Count == 0)
        {
            return "无历史记录";
        }

        var sb = new StringBuilder();
        sb.AppendLine("最近执行历史:");

        foreach (var record in records)
        {
            var statusMarker = GetStatusMarker(record.TreeStatus);
            sb.AppendLine($"  帧 {record.FrameNumber}: {statusMarker} {record.TreeStatus} ({record.TotalExecutionTimeMs:F2}ms)");
        }

        return sb.ToString();
    }

    #endregion

    #region 私有方法

    private void RenderNode(
        IBTNode node,
        StringBuilder sb,
        string prefix,
        bool isLast,
        IReadOnlyDictionary<string, BTNodeSnapshot>? snapshots,
        HashSet<string>? activeNames = null)
    {
        var connector = isLast ? _config.LastBranch : _config.Branch;
        var snapshot = snapshots?.GetValueOrDefault(node.Name);
        var isActive = activeNames?.Contains(node.Name) ?? false;

        var statusStr = "";
        if (_config.ShowStatus && snapshot is not null)
        {
            statusStr = $" {GetStatusMarker(snapshot.Value.Status)}";
        }

        var activeStr = _config.HighlightActivePath && isActive ? $" {_config.ActiveMarker}" : "";

        var typeStr = "";
        if (_config.ShowNodeType)
        {
            typeStr = $" [{node.NodeType}]";
        }

        var childStr = "";
        if (_config.ShowChildCount)
        {
            var childCount = GetChildCount(node);
            if (childCount > 0)
            {
                childStr = $" ({childCount} 子节点)";
            }
        }

        sb.AppendLine($"{prefix}{connector}{node.Name}{typeStr}{statusStr}{activeStr}{childStr}");

        var childPrefix = prefix + (isLast ? _config.Indent : _config.VerticalLine);

        if (node is CompositeNode composite)
        {
            for (var i = 0; i < composite.Children.Count; i++)
            {
                RenderNode(composite.Children[i], sb, childPrefix, i == composite.Children.Count - 1, snapshots, activeNames);
            }
        }
        else if (node is DecoratorNode decorator && decorator.Child is not null)
        {
            RenderNode(decorator.Child, sb, childPrefix, true, snapshots, activeNames);
        }
    }

    private string GetStatusMarker(BTNodeStatus status)
    {
        return status switch
        {
            BTNodeStatus.Success => _config.SuccessMarker,
            BTNodeStatus.Failure => _config.FailureMarker,
            BTNodeStatus.Running => _config.RunningMarker,
            _ => "?"
        };
    }

    private static int GetChildCount(IBTNode node)
    {
        if (node is CompositeNode composite)
        {
            return composite.Children.Count;
        }

        if (node is DecoratorNode decorator)
        {
            return decorator.Child is not null ? 1 : 0;
        }

        return 0;
    }

    private static List<string> GetBlackboardKeys(Blackboard.Blackboard blackboard)
    {
        var keysField = typeof(Blackboard.Blackboard).GetField("_values",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (keysField?.GetValue(blackboard) is Dictionary<string, object> values)
        {
            return values.Keys.ToList();
        }

        return new List<string>();
    }

    #endregion
}
