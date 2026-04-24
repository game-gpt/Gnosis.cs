using Gnosis.Widget.Element;
using Gnosis.Widget.Render;

namespace Gnosis.Widget.NodeEditor;

public sealed class NodeCanvas : ContainerElement
{
    #region 常量

    private const float MinZoom = 0.25f;
    private const float MaxZoom = 4.0f;
    private const float ZoomStep = 0.1f;
    private const float GridSize = 20.0f;

    #endregion

    #region 状态

    private float _offsetX;
    private float _offsetY;
    private float _zoom = 1.0f;
    private bool _isPanning;
    private float _panStartX;
    private float _panStartY;
    private string? _dragNodeId;
    private float _dragNodeStartX;
    private float _dragNodeStartY;
    private float _dragMouseStartX;
    private float _dragMouseStartY;
    private string? _connectingSourceNodeId;
    private string? _connectingSourcePortName;
    private float _connectingMouseX;
    private float _connectingMouseY;
    private string? _selectedNodeId;

    #endregion

    #region 依赖

    private readonly NodeGraph _graph;

    #endregion

    #region 事件

    public event Action<string?>? SelectedNodeChanged;
    public event Action<string, string, string, string>? ConnectionCreated;

    #endregion

    #region 属性

    public float Zoom => _zoom;
    public string? SelectedNodeId => _selectedNodeId;

    #endregion

    #region 构造函数

    public NodeCanvas(NodeGraph graph)
    {
        _graph = graph;
        IsFocusable = true;
        Background = new Color(0.15f, 0.15f, 0.18f, 1.0f);
    }

    #endregion

    #region 坐标转换

    private (float worldX, float worldY) ScreenToWorld(float screenX, float screenY)
    {
        var contentRect = LayoutRect.Deflate(Margin).Deflate(Border).Deflate(Padding);
        var localX = screenX - contentRect.X;
        var localY = screenY - contentRect.Y;
        var worldX = (localX - _offsetX) / _zoom;
        var worldY = (localY - _offsetY) / _zoom;
        return (worldX, worldY);
    }

    private (float screenX, float screenY) WorldToScreen(float worldX, float worldY)
    {
        var contentRect = LayoutRect.Deflate(Margin).Deflate(Border).Deflate(Padding);
        var screenX = worldX * _zoom + _offsetX + contentRect.X;
        var screenY = worldY * _zoom + _offsetY + contentRect.Y;
        return (screenX, screenY);
    }

    #endregion

    #region 布局

    protected override Size MeasureChildren(Size availableSize)
    {
        return new Size(availableSize.Width, availableSize.Height);
    }

    protected override void ArrangeChildren(Rect contentRect)
    {
        foreach (var child in Children)
        {
            child.Arrange(contentRect);
        }
    }

    #endregion

    #region 渲染

    public override void Paint(IWidgetRenderer renderer)
    {
        if (Background.A > 0)
        {
            renderer.DrawRect(
                LayoutRect.X, LayoutRect.Y,
                LayoutRect.Width, LayoutRect.Height,
                Background.R, Background.G, Background.B, Background.A
            );
        }

        var contentRect = LayoutRect.Deflate(Margin).Deflate(Border).Deflate(Padding);

        renderer.PushClip(contentRect.X, contentRect.Y, contentRect.Width, contentRect.Height);

        PaintGrid(renderer, contentRect);
        PaintConnections(renderer, contentRect);
        PaintNodes(renderer, contentRect);

        if (_connectingSourceNodeId is not null)
        {
            PaintPendingConnection(renderer, contentRect);
        }

        renderer.PopClip();
    }

    private void PaintGrid(IWidgetRenderer renderer, Rect contentRect)
    {
        var gridColor = new Color(0.22f, 0.22f, 0.26f, 1.0f);
        var majorGridColor = new Color(0.28f, 0.28f, 0.32f, 1.0f);

        var startX = _offsetX % (GridSize * _zoom);
        var startY = _offsetY % (GridSize * _zoom);

        for (var x = startX; x < contentRect.Width; x += GridSize * _zoom)
        {
            var isMajor = Math.Abs((x - _offsetX) / _zoom) % (GridSize * 5) < 0.5f;
            var color = isMajor ? majorGridColor : gridColor;
            renderer.DrawLine(
                contentRect.X + x, contentRect.Y,
                contentRect.X + x, contentRect.Bottom,
                color.R, color.G, color.B, 0.5f
            );
        }

        for (var y = startY; y < contentRect.Height; y += GridSize * _zoom)
        {
            var isMajor = Math.Abs((y - _offsetY) / _zoom) % (GridSize * 5) < 0.5f;
            var color = isMajor ? majorGridColor : gridColor;
            renderer.DrawLine(
                contentRect.X, contentRect.Y + y,
                contentRect.Right, contentRect.Y + y,
                color.R, color.G, color.B, 0.5f
            );
        }
    }

    private void PaintNodes(IWidgetRenderer renderer, Rect contentRect)
    {
        foreach (var (nodeId, node) in _graph.Nodes)
        {
            var (sx, sy) = WorldToScreen(node.X, node.Y);
            var sw = node.Width * _zoom;
            var sh = CalculateNodeHeight(node) * _zoom;

            if (sx + sw < contentRect.X || sx > contentRect.Right ||
                sy + sh < contentRect.Y || sy > contentRect.Bottom)
            {
                continue;
            }

            var isSelected = nodeId == _selectedNodeId;
            var headerColor = GetCategoryColor(node.Category);
            var bodyColor = new Color(0.2f, 0.2f, 0.24f, 0.95f);

            if (isSelected)
            {
                renderer.DrawRect(sx - 2, sy - 2, sw + 4, sh + 4,
                    0.3f, 0.6f, 1.0f, 1.0f);
            }

            renderer.DrawRect(sx, sy, sw, sh,
                bodyColor.R, bodyColor.G, bodyColor.B, bodyColor.A);

            var headerHeight = 24 * _zoom;
            renderer.DrawRect(sx, sy, sw, headerHeight,
                headerColor.R, headerColor.G, headerColor.B, headerColor.A);

            renderer.DrawText(node.Title, sx + 8 * _zoom, sy + 4 * _zoom, 10 * _zoom,
                1.0f, 1.0f, 1.0f);

            var portY = sy + headerHeight + 8 * _zoom;
            var portRadius = 5 * _zoom;

            foreach (var port in node.InputPorts)
            {
                var px = sx + 10 * _zoom;
                var py = portY + portRadius;

                var portColor = GetPortColor(port.Type);
                renderer.DrawRect(px - portRadius, py - portRadius, portRadius * 2, portRadius * 2,
                    portColor.R, portColor.G, portColor.B, portColor.A);

                renderer.DrawText(port.Name, px + portRadius + 4 * _zoom, py - 4 * _zoom, 8 * _zoom,
                    0.8f, 0.8f, 0.8f);

                portY += 18 * _zoom;
            }

            portY = sy + headerHeight + 8 * _zoom;

            foreach (var port in node.OutputPorts)
            {
                var px = sx + sw - 10 * _zoom;
                var py = portY + portRadius;

                var portColor = GetPortColor(port.Type);
                renderer.DrawRect(px - portRadius, py - portRadius, portRadius * 2, portRadius * 2,
                    portColor.R, portColor.G, portColor.B, portColor.A);

                var textWidth = port.Name.Length * 6 * _zoom;
                renderer.DrawText(port.Name, px - portRadius - textWidth - 4 * _zoom, py - 4 * _zoom, 8 * _zoom,
                    0.8f, 0.8f, 0.8f);

                portY += 18 * _zoom;
            }
        }
    }

    private void PaintConnections(IWidgetRenderer renderer, Rect contentRect)
    {
        foreach (var conn in _graph.Connections)
        {
            if (!_graph.Nodes.TryGetValue(conn.SourceNodeId, out var sourceNode) ||
                !_graph.Nodes.TryGetValue(conn.TargetNodeId, out var targetNode))
            {
                continue;
            }

            var sourcePortIndex = sourceNode.OutputPorts.FindIndex(p => p.Name == conn.SourcePortName);
            var targetPortIndex = targetNode.InputPorts.FindIndex(p => p.Name == conn.TargetPortName);

            if (sourcePortIndex < 0 || targetPortIndex < 0)
            {
                continue;
            }

            var (sx, sy) = WorldToScreen(sourceNode.X + sourceNode.Width, sourceNode.Y);
            var headerHeight = 24 * _zoom;
            sy += headerHeight + 8 * _zoom + sourcePortIndex * 18 * _zoom + 5 * _zoom;

            var (ex, ey) = WorldToScreen(targetNode.X, targetNode.Y);
            ey += headerHeight + 8 * _zoom + targetPortIndex * 18 * _zoom + 5 * _zoom;

            var midX = (sx + ex) / 2;

            var portType = sourceNode.OutputPorts[sourcePortIndex].Type;
            var color = GetPortColor(portType);

            DrawBezierConnection(renderer, sx, sy, midX, ex, ey, midX, color);
        }
    }

    private void PaintPendingConnection(IWidgetRenderer renderer, Rect contentRect)
    {
        if (!_graph.Nodes.TryGetValue(_connectingSourceNodeId!, out var sourceNode))
        {
            return;
        }

        var sourcePortIndex = sourceNode.OutputPorts.FindIndex(p => p.Name == _connectingSourcePortName);
        if (sourcePortIndex < 0)
        {
            return;
        }

        var (sx, sy) = WorldToScreen(sourceNode.X + sourceNode.Width, sourceNode.Y);
        var headerHeight = 24 * _zoom;
        sy += headerHeight + 8 * _zoom + sourcePortIndex * 18 * _zoom + 5 * _zoom;

        var (ex, ey) = (_connectingMouseX, _connectingMouseY);
        var midX = (sx + ex) / 2;

        var portType = sourceNode.OutputPorts[sourcePortIndex].Type;
        var color = GetPortColor(portType);

        DrawBezierConnection(renderer, sx, sy, midX, ex, ey, midX, color);
    }

    private void DrawBezierConnection(IWidgetRenderer renderer,
        float sx, float sy, float cpx1, float ex, float ey, float cpx2, Color color)
    {
        const int segments = 16;

        for (var i = 0; i < segments; i++)
        {
            var t0 = (float)i / segments;
            var t1 = (float)(i + 1) / segments;

            var x0 = CubicBezier(sx, cpx1, cpx2, ex, t0);
            var y0 = CubicBezier(sy, sy, ey, ey, t0);
            var x1 = CubicBezier(sx, cpx1, cpx2, ex, t1);
            var y1 = CubicBezier(sy, sy, ey, ey, t1);

            renderer.DrawLine(x0, y0, x1, y1, color.R, color.G, color.B, color.A, 2.0f);
        }
    }

    private static float CubicBezier(float p0, float p1, float p2, float p3, float t)
    {
        var mt = 1.0f - t;
        return mt * mt * mt * p0 + 3 * mt * mt * t * p1 + 3 * mt * t * t * p2 + t * t * t * p3;
    }

    #endregion

    #region 交互

    public void HandleMouseDown(float x, float y, MouseButton button)
    {
        var (worldX, worldY) = ScreenToWorld(x, y);

        if (button == MouseButton.Middle)
        {
            _isPanning = true;
            _panStartX = x;
            _panStartY = y;
            return;
        }

        if (button == MouseButton.Left)
        {
            var clickedNode = FindNodeAt(worldX, worldY);

            if (clickedNode is not null)
            {
                var portHit = FindPortAt(clickedNode, worldX, worldY);

                if (portHit is { Direction: PortDirection.Output })
                {
                    _connectingSourceNodeId = clickedNode.Id;
                    _connectingSourcePortName = portHit.Name;
                    _connectingMouseX = x;
                    _connectingMouseY = y;
                    return;
                }

                _selectedNodeId = clickedNode.Id;
                _dragNodeId = clickedNode.Id;
                _dragNodeStartX = clickedNode.X;
                _dragNodeStartY = clickedNode.Y;
                _dragMouseStartX = worldX;
                _dragMouseStartY = worldY;
                SelectedNodeChanged?.Invoke(_selectedNodeId);
            }
            else
            {
                _selectedNodeId = null;
                SelectedNodeChanged?.Invoke(null);
                _isPanning = true;
                _panStartX = x;
                _panStartY = y;
            }
        }
    }

    public void HandleMouseMove(float x, float y)
    {
        if (_isPanning)
        {
            _offsetX += x - _panStartX;
            _offsetY += y - _panStartY;
            _panStartX = x;
            _panStartY = y;
            return;
        }

        if (_dragNodeId is not null && _graph.Nodes.TryGetValue(_dragNodeId, out var node))
        {
            var (worldX, worldY) = ScreenToWorld(x, y);
            node.X = _dragNodeStartX + (worldX - _dragMouseStartX);
            node.Y = _dragNodeStartY + (worldY - _dragMouseStartY);
            return;
        }

        if (_connectingSourceNodeId is not null)
        {
            _connectingMouseX = x;
            _connectingMouseY = y;
        }
    }

    public void HandleMouseUp(float x, float y, MouseButton button)
    {
        if (button == MouseButton.Middle)
        {
            _isPanning = false;
            return;
        }

        if (button == MouseButton.Left)
        {
            if (_connectingSourceNodeId is not null)
            {
                var (worldX, worldY) = ScreenToWorld(x, y);
                var targetNode = FindNodeAt(worldX, worldY);

                if (targetNode is not null && targetNode.Id != _connectingSourceNodeId)
                {
                    var targetPort = FindPortAt(targetNode, worldX, worldY);

                    if (targetPort is { Direction: PortDirection.Input })
                    {
                        ConnectionCreated?.Invoke(
                            _connectingSourceNodeId, _connectingSourcePortName!,
                            targetNode.Id, targetPort.Name);

                        _graph.AddConnection(
                            _connectingSourceNodeId, _connectingSourcePortName!,
                            targetNode.Id, targetPort.Name);
                    }
                }

                _connectingSourceNodeId = null;
                _connectingSourcePortName = null;
            }

            _dragNodeId = null;
            _isPanning = false;
        }
    }

    public void HandleWheel(float x, float y, float delta)
    {
        var oldZoom = _zoom;
        _zoom = Math.Clamp(_zoom + (delta > 0 ? ZoomStep : -ZoomStep), MinZoom, MaxZoom);

        var contentRect = LayoutRect.Deflate(Margin).Deflate(Border).Deflate(Padding);
        var localX = x - contentRect.X;
        var localY = y - contentRect.Y;

        _offsetX = localX - (localX - _offsetX) * (_zoom / oldZoom);
        _offsetY = localY - (localY - _offsetY) * (_zoom / oldZoom);
    }

    #endregion

    #region 辅助方法

    private EditorNode? FindNodeAt(float worldX, float worldY)
    {
        foreach (var (_, node) in _graph.Nodes)
        {
            var height = CalculateNodeHeight(node);

            if (worldX >= node.X && worldX <= node.X + node.Width &&
                worldY >= node.Y && worldY <= node.Y + height)
            {
                return node;
            }
        }

        return null;
    }

    private NodePort? FindPortAt(EditorNode node, float worldX, float worldY)
    {
        var headerHeight = 24;
        var portStartY = node.Y + headerHeight + 8;
        var portRadius = 5;

        for (var i = 0; i < node.InputPorts.Count; i++)
        {
            var port = node.InputPorts[i];
            var px = node.X + 10;
            var py = portStartY + i * 18 + portRadius;

            if (Math.Abs(worldX - px) < portRadius * 2 && Math.Abs(worldY - py) < portRadius * 2)
            {
                return port;
            }
        }

        for (var i = 0; i < node.OutputPorts.Count; i++)
        {
            var port = node.OutputPorts[i];
            var px = node.X + node.Width - 10;
            var py = portStartY + i * 18 + portRadius;

            if (Math.Abs(worldX - px) < portRadius * 2 && Math.Abs(worldY - py) < portRadius * 2)
            {
                return port;
            }
        }

        return null;
    }

    private static float CalculateNodeHeight(EditorNode node)
    {
        var headerHeight = 24;
        var maxPorts = Math.Max(node.InputPorts.Count, node.OutputPorts.Count);
        var portsHeight = maxPorts * 18;
        return headerHeight + 8 + portsHeight + 8;
    }

    private static Color GetCategoryColor(string category)
    {
        return category switch
        {
            "Dialogue" => new Color(0.2f, 0.5f, 0.8f, 1.0f),
            "Scene" => new Color(0.5f, 0.7f, 0.3f, 1.0f),
            "Character" => new Color(0.8f, 0.4f, 0.6f, 1.0f),
            "Effect" => new Color(0.7f, 0.5f, 0.9f, 1.0f),
            "Audio" => new Color(0.9f, 0.6f, 0.2f, 1.0f),
            "Quest" => new Color(0.9f, 0.8f, 0.2f, 1.0f),
            "Control" => new Color(0.6f, 0.6f, 0.6f, 1.0f),
            "Variable" => new Color(0.4f, 0.8f, 0.7f, 1.0f),
            _ => new Color(0.4f, 0.4f, 0.5f, 1.0f)
        };
    }

    private static Color GetPortColor(PortType type)
    {
        return type switch
        {
            PortType.Flow => new Color(1.0f, 1.0f, 1.0f, 1.0f),
            PortType.String => new Color(0.9f, 0.6f, 0.2f, 1.0f),
            PortType.Number => new Color(0.3f, 0.7f, 1.0f, 1.0f),
            PortType.Boolean => new Color(0.9f, 0.2f, 0.2f, 1.0f),
            _ => new Color(0.7f, 0.7f, 0.7f, 1.0f)
        };
    }

    #endregion
}
