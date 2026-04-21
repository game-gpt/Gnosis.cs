namespace Gnosis.ECS;

public enum SystemPhase
{
    Initialization,
    PreUpdate,
    Update,
    PostUpdate,
    PreRender,
    Render,
    PostRender,
    Cleanup
}
