namespace Gnosis.Scene.Graph;

public interface ISceneNode
{
    string Name { get; }
    ISceneNode? Parent { get; }
    IReadOnlyList<ISceneNode> Children { get; }
    bool IsDirty { get; }
    void AddChild(ISceneNode child);
    void RemoveChild(string name);
    ISceneNode? FindChild(string name);
    void MarkDirty();
    void ClearDirty();
}
