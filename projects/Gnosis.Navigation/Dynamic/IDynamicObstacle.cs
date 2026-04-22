namespace Gnosis.Navigation.Dynamic;

/// <summary>
/// 动态障碍物接口，支持运行时导航网格更新
/// </summary>
public interface IDynamicObstacle
{
    /// <summary>
    /// 障碍物名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 障碍物形状
    /// </summary>
    ObstacleShape Shape { get; }

    /// <summary>
    /// 障碍物位置
    /// </summary>
    float[] Position { get; }

    /// <summary>
    /// 是否激活
    /// </summary>
    bool IsActive { get; set; }

    /// <summary>
    /// 更新障碍物位置
    /// </summary>
    void UpdatePosition(float[] position);

    /// <summary>
    /// 获取障碍物占据的导航区域
    /// </summary>
    float[] GetBounds();
}
