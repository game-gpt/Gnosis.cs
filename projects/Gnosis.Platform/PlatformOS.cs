namespace Gnosis.Platform;

/// <summary>
/// 操作系统：决定系统调用、窗口系统、图形 API
/// 编译期确定，无法通过插件修改
/// </summary>
public enum PlatformOS
{
    Windows,
    Linux,
    macOS,
    Android,
    iOS,
    Web,
    PlayStation,
    Xbox,
    Switch
}
