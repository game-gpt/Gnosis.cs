using System.Numerics;

namespace Gnosis.Scene.Graph;

public sealed class SceneNode : ISceneNode
{
    #region 字段

    private readonly List<ISceneNode> _children = new();
    private readonly Dictionary<string, ISceneNode> _childrenByName = new();
    private Vector2 _position;

    #endregion

    #region 属性

    public string Name { get; }

    public ISceneNode? Parent { get; private set; }

    public IReadOnlyList<ISceneNode> Children => _children;

    public Vector2 Position
    {
        get => _position;
        set
        {
            _position = value;
            IsDirty = true;
        }
    }

    public bool IsDirty { get; private set; }

    #endregion

    #region 构造函数

    public SceneNode(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    #endregion

    #region ISceneNode 实现

    public void AddChild(ISceneNode child)
    {
        if (child is null)
        {
            throw new ArgumentNullException(nameof(child));
        }

        _children.Add(child);
        _childrenByName[child.Name] = child;

        if (child is SceneNode node)
        {
            node.Parent = this;
        }

        MarkDirty();
    }

    public void RemoveChild(string name)
    {
        if (_childrenByName.TryGetValue(name, out var child))
        {
            _children.Remove(child);
            _childrenByName.Remove(name);

            if (child is SceneNode node)
            {
                node.Parent = null;
            }

            MarkDirty();
        }
    }

    public ISceneNode? FindChild(string name)
    {
        return _childrenByName.TryGetValue(name, out var child) ? child : null;
    }

    public void MarkDirty()
    {
        IsDirty = true;
    }

    public void ClearDirty()
    {
        IsDirty = false;
    }

    #endregion
}
