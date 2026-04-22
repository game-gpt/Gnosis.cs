namespace Gnosis.Navigation.Link;

/// <summary>
/// 离网连接类型
/// </summary>
public enum NavLinkType : byte
{
    Jump = 0,
    Drop = 1,
    Teleport = 2,
    Ladder = 3,
    Custom = 4
}
