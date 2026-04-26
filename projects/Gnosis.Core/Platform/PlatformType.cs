namespace Gnosis.Core.Platform;

/// <summary>
/// 平台类型枚举（已过时）
/// 请使用 Gnosis.Platform.PlatformArchitecture 替代
/// 平台模型已重构为 Architecture + Channel = Platform
/// </summary>
[Obsolete("请使用 Gnosis.Platform.PlatformArchitecture 替代。平台模型已重构为 Architecture + Channel = Platform")]
public enum PlatformType
{
    Windows,
    Linux,
    macOS,
    iOS,
    Android,
    WebAssembly,
    PlayStation,
    Xbox,
    Switch
}
