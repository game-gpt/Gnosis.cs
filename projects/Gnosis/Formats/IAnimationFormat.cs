namespace Gnosis.Formats;

public interface IAnimationFormat : IFormatHandler
{
    Task<AnimationData> LoadAnimationAsync(string path, CancellationToken cancellationToken = default);
    Task SaveAnimationAsync(string path, AnimationData animation, CancellationToken cancellationToken = default);
}

public record AnimationData
{
    public string Name { get; init; } = string.Empty;
    public float Duration { get; init; }
    public float TickRate { get; init; } = 30.0f;
    public IReadOnlyList<AnimationTrack> Tracks { get; init; } = new List<AnimationTrack>();
    public IReadOnlyList<AnimationEvent> Events { get; init; } = new List<AnimationEvent>();
}

public record AnimationTrack
{
    public string TargetPath { get; init; } = string.Empty;
    public string PropertyName { get; init; } = string.Empty;
    AnimationTrackType Type { get; init; }
    public IReadOnlyList<AnimationKeyframe> Keyframes { get; init; } = new List<AnimationKeyframe>();
}

public record AnimationKeyframe
{
    public float Time { get; init; }
    public object Value { get; init; } = new object();
    public float[] InTangent { get; init; } = Array.Empty<float>();
    public float[] OutTangent { get; init; } = Array.Empty<float>();
    public AnimationInterpolation Interpolation { get; init; }
}

public record AnimationEvent
{
    public float Time { get; init; }
    public string EventName { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, object> Parameters { get; init; } = new Dictionary<string, object>();
}

public enum AnimationTrackType
{
    Translation = 0,
    Rotation = 1,
    Scale = 2,
    MorphTarget = 3,
    Custom = 4
}

public enum AnimationInterpolation
{
    Step = 0,
    Linear = 1,
    Cubic = 2,
    Bezier = 3
}
