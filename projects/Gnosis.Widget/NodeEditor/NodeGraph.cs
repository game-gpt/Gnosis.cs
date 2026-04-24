namespace Gnosis.Widget.NodeEditor;

public enum PortDirection
{
    Input,
    Output
}

public enum PortType
{
    Flow,
    String,
    Number,
    Boolean,
    Any
}

public sealed class NodePort
{
    public string Name { get; }
    public PortDirection Direction { get; }
    public PortType Type { get; }
    public object? Value { get; set; }

    public NodePort(string name, PortDirection direction, PortType type = PortType.Any, object? value = null)
    {
        Name = name;
        Direction = direction;
        Type = type;
        Value = value;
    }
}

public sealed class NodeConnection
{
    public string SourceNodeId { get; }
    public string SourcePortName { get; }
    public string TargetNodeId { get; }
    public string TargetPortName { get; }

    public NodeConnection(string sourceNodeId, string sourcePortName, string targetNodeId, string targetPortName)
    {
        SourceNodeId = sourceNodeId;
        SourcePortName = sourcePortName;
        TargetNodeId = targetNodeId;
        TargetPortName = targetPortName;
    }

    public override bool Equals(object? obj)
    {
        return obj is NodeConnection other &&
               SourceNodeId == other.SourceNodeId &&
               SourcePortName == other.SourcePortName &&
               TargetNodeId == other.TargetNodeId &&
               TargetPortName == other.TargetPortName;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SourceNodeId, SourcePortName, TargetNodeId, TargetPortName);
    }
}

public sealed class EditorNode
{
    public string Id { get; }
    public string Title { get; set; }
    public string Category { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }
    public List<NodePort> InputPorts { get; } = [];
    public List<NodePort> OutputPorts { get; } = [];
    public Dictionary<string, object> Properties { get; } = new();

    public EditorNode(string id, string title, string category = "Default")
    {
        Id = id;
        Title = title;
        Category = category;
        Width = 180;
        Height = 60;
    }

    public NodePort AddInput(string name, PortType type = PortType.Any, object? value = null)
    {
        var port = new NodePort(name, PortDirection.Input, type, value);
        InputPorts.Add(port);
        return port;
    }

    public NodePort AddOutput(string name, PortType type = PortType.Any)
    {
        var port = new NodePort(name, PortDirection.Output, type);
        OutputPorts.Add(port);
        return port;
    }
}

public sealed class NodeGraph
{
    private readonly Dictionary<string, EditorNode> _nodes = new(StringComparer.Ordinal);
    private readonly List<NodeConnection> _connections = [];

    public IReadOnlyDictionary<string, EditorNode> Nodes => _nodes;
    public IReadOnlyList<NodeConnection> Connections => _connections;

    public event Action? GraphChanged;

    public EditorNode AddNode(EditorNode node)
    {
        _nodes[node.Id] = node;
        GraphChanged?.Invoke();
        return node;
    }

    public void RemoveNode(string nodeId)
    {
        _nodes.Remove(nodeId);
        _connections.RemoveAll(c =>
            c.SourceNodeId == nodeId || c.TargetNodeId == nodeId);
        GraphChanged?.Invoke();
    }

    public NodeConnection? AddConnection(string sourceNodeId, string sourcePortName, string targetNodeId, string targetPortName)
    {
        if (!_nodes.TryGetValue(sourceNodeId, out var sourceNode) ||
            !_nodes.TryGetValue(targetNodeId, out var targetNode))
        {
            return null;
        }

        if (sourceNode.OutputPorts.All(p => p.Name != sourcePortName) ||
            targetNode.InputPorts.All(p => p.Name != targetPortName))
        {
            return null;
        }

        _connections.RemoveAll(c =>
            c.TargetNodeId == targetNodeId && c.TargetPortName == targetPortName);

        var connection = new NodeConnection(sourceNodeId, sourcePortName, targetNodeId, targetPortName);
        _connections.Add(connection);
        GraphChanged?.Invoke();
        return connection;
    }

    public void RemoveConnection(string sourceNodeId, string sourcePortName, string targetNodeId, string targetPortName)
    {
        _connections.RemoveAll(c =>
            c.SourceNodeId == sourceNodeId && c.SourcePortName == sourcePortName &&
            c.TargetNodeId == targetNodeId && c.TargetPortName == targetPortName);
        GraphChanged?.Invoke();
    }

    public void Clear()
    {
        _nodes.Clear();
        _connections.Clear();
        GraphChanged?.Invoke();
    }
}
