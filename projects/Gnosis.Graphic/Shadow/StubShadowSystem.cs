using Gnosis.Graphic.Light;

namespace Gnosis.Graphic.Shadow;

public class StubShadowSystem : IShadowSystem
{
    public IShadowSettings Settings { get => throw new NotImplementedException("光照系统尚未实现"); set => throw new NotImplementedException("光照系统尚未实现"); }
    public void RenderShadowMaps() { throw new NotImplementedException("光照系统尚未实现"); }
    public void UpdateCascades(IDirectionalLight light) { throw new NotImplementedException("光照系统尚未实现"); }
}
