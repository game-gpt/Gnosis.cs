namespace Gnosis.ECS.System;

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
