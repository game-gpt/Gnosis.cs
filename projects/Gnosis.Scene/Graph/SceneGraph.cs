namespace Gnosis.Scene.Graph;

public sealed class SceneGraph
{
    #region 属性

    public ISceneNode Root { get; }

    #endregion

    #region 构造函数

    public SceneGraph(string rootName = "Root")
    {
        Root = new SceneNode(rootName);
    }

    #endregion

    #region 公开方法

    public ISceneNode? FindNode(string path)
    {
        var parts = path.Split('/');
        var current = Root;

        foreach (var part in parts)
        {
            var child = current.FindChild(part);

            if (child is null)
            {
                return null;
            }

            current = child;
        }

        return current;
    }

    public void PropagateDirty()
    {
        PropagateDirtyRecursive(Root);
    }

    #endregion

    #region 私有方法

    private static void PropagateDirtyRecursive(ISceneNode node)
    {
        if (!node.IsDirty)
        {
            return;
        }

        foreach (var child in node.Children)
        {
            child.MarkDirty();
            PropagateDirtyRecursive(child);
        }

        node.ClearDirty();
    }

    #endregion
}
