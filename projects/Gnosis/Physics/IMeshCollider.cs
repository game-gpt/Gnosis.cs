namespace Gnosis.Physics;

public interface IMeshCollider : ICollider
{
    string MeshPath { get; set; }
    bool IsConvex { get; set; }
    bool IsCooked { get; }
    void CookMesh();
}
