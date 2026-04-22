namespace Gnosis.Core.Platform;

/// <summary>
/// 表示 CPU 架构类型的枚举
/// </summary>
public enum SystemArchitecture
{
    /// <summary>
    /// 未知架构
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// x64 架构
    /// </summary>
    X64 = 1,

    /// <summary>
    /// x86 架构
    /// </summary>
    X86 = 2,

    /// <summary>
    /// ARM64 架构
    /// </summary>
    Arm64 = 3,

    /// <summary>
    /// ARM 架构
    /// </summary>
    Arm = 4,

    /// <summary>
    /// WebAssembly 架构
    /// </summary>
    Wasm = 5
}
