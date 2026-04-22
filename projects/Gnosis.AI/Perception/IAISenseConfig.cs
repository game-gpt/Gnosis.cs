namespace Gnosis.AI.Perception;

public interface IAISenseConfig
{
    AISenseType SenseType { get; }
    bool IsEnabled { get; set; }
    float Range { get; set; }
    float Interval { get; set; }
}
