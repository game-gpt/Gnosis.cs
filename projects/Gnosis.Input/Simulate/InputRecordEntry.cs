using Gnosis.Input.Device;

namespace Gnosis.Input.Simulate;

public struct InputRecordEntry
{
    public float Timestamp { get; init; }
    public InputDeviceType DeviceType { get; init; }
    public string Action { get; init; }
    public float Value { get; init; }
}
