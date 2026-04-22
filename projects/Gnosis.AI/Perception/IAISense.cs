namespace Gnosis.AI.Perception;

public interface IAISense
{
    AISenseType SenseType { get; }
    void Update(float delta);
    void RegisterTarget(IAIStimulusSource source);
    void UnregisterTarget(IAIStimulusSource source);
}

public interface IAIStimulusSource
{
    float[] Position { get; }
    float Strength { get; }
    AISenseType SenseType { get; }
    bool IsActive { get; }
}
