namespace Gnosis.AI.Perception;

public class StubAIPerception : IAIPerception
{
    public IReadOnlyList<IAISenseConfig> SenseConfigs => throw new NotImplementedException("AI 系统尚未实现");
    public IReadOnlyList<IAIStimulusSource> PerceivedTargets => throw new NotImplementedException("AI 系统尚未实现");
    public void AddSense(IAISenseConfig config) { throw new NotImplementedException("AI 系统尚未实现"); }
    public float[] GetLastKnownPosition(IAIStimulusSource target) { throw new NotImplementedException("AI 系统尚未实现"); }
    public bool IsTargetPerceived(IAIStimulusSource target) { throw new NotImplementedException("AI 系统尚未实现"); }
    public void RemoveSense(AISenseType senseType) { throw new NotImplementedException("AI 系统尚未实现"); }
    public void Update(float delta) { throw new NotImplementedException("AI 系统尚未实现"); }
}
