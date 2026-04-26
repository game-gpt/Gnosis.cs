namespace Gnosis.Platform;

/// <summary>
/// 平台架构层：底层操作系统/运行环境
/// 编译期确定，无法通过插件修改，需要改编译器
/// </summary>
public enum PlatformArchitecture
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
