namespace Gnosis.Platform;

/// <summary>
/// 平台渠道层：分发渠道，在架构之上添加自定义能力
/// 插件可通过 [js("wx.login")] micro wechat_login(...) 方式定义渠道能力
/// </summary>
public enum PlatformChannel
{
    /// <summary>
    /// 默认渠道：无渠道能力，架构名即平台名
    /// </summary>
    Default,

    /// <summary>
    /// Steam 渠道：Windows/Linux/macOS 上的 Steam 分发
    /// </summary>
    Steam,

    /// <summary>
    /// 微信渠道：任意架构下的微信小游戏/小程序
    /// </summary>
    WeChat,

    /// <summary>
    /// Epic Games 渠道
    /// </summary>
    EpicGames,

    /// <summary>
    /// Apple App Store 渠道
    /// </summary>
    AppleAppStore,

    /// <summary>
    /// Google Play 渠道
    /// </summary>
    GooglePlay
}
