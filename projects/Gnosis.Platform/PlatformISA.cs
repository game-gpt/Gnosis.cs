namespace Gnosis.Platform;

/// <summary>
/// 指令集架构：决定二进制格式、JIT 后端、原生库加载
/// 编译期确定，无法通过插件修改
/// </summary>
public enum PlatformISA
{
    X64,
    Arm64,
    Wasm
}
