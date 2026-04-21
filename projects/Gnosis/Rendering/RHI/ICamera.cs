namespace Gnosis.Rendering.RHI;

public interface ICamera : IView
{
    float FieldOfView { get; set; }
    float NearPlane { get; set; }
    float FarPlane { get; set; }
    float AspectRatio { get; set; }
}
