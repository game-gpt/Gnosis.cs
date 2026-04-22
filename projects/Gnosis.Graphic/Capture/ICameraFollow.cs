namespace Gnosis.Graphic.Capture;

public interface ICameraFollow
{
    float[] TargetPosition { get; set; }
    float[] Offset { get; set; }
    float FollowSpeed { get; set; }
    float Damping { get; set; }
    bool IsFollowing { get; }
    void StartFollowing();
    void StopFollowing();
    void Update(float delta);
}
