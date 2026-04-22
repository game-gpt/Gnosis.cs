using Gnosis.Scene.Graph;

namespace Gnosis.Scene.Prefab;

public sealed class Prefab
{
    #region 属性

    public string Name { get; }

    public ISceneNode RootNode { get; }

    #endregion

    #region 构造函数

    public Prefab(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        RootNode = new SceneNode(name);
    }

    #endregion

    #region 公开方法

    public ISceneNode Instantiate()
    {
        return DeepClone(RootNode);
    }

    #endregion

    #region 私有方法

    private static ISceneNode DeepClone(ISceneNode source)
    {
        var clone = new SceneNode(source.Name);

        foreach (var child in source.Children)
        {
            clone.AddChild(DeepClone(child));
        }

        return clone;
    }

    #endregion
}
