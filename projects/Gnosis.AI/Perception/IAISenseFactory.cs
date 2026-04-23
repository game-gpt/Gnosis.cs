namespace Gnosis.AI.Perception;

/// <summary>
/// 感知通道工厂接口，用于根据配置创建对应的感知通道实例
/// </summary>
public interface IAISenseFactory
{
    /// <summary>
    /// 判断工厂是否支持指定的配置类型
    /// </summary>
    /// <param name="configType">配置类型</param>
    /// <returns>是否支持</returns>
    bool CanCreate(Type configType);

    /// <summary>
    /// 根据配置创建感知通道
    /// </summary>
    /// <param name="config">感知配置</param>
    /// <returns>感知通道实例</returns>
    IAISense Create(IAISenseConfig config);
}

/// <summary>
/// 默认感知通道工厂，内置支持 Sight、Hearing、Touch 三种感知类型
/// </summary>
public sealed class DefaultAISenseFactory : IAISenseFactory
{
    /// <summary>
    /// 单例实例
    /// </summary>
    public static readonly DefaultAISenseFactory Instance = new();

    private DefaultAISenseFactory()
    {
    }

    /// <inheritdoc />
    public bool CanCreate(Type configType)
    {
        return configType == typeof(AISightConfig)
               || configType == typeof(AIHearingConfig)
               || configType == typeof(AITouchConfig);
    }

    /// <inheritdoc />
    public IAISense Create(IAISenseConfig config)
    {
        return config switch
        {
            AISightConfig sightConfig => new AISight(sightConfig),
            AIHearingConfig hearingConfig => new AIHearing(hearingConfig),
            AITouchConfig touchConfig => new AITouch(touchConfig),
            _ => throw new ArgumentException($"不支持的感知配置类型: {config.GetType().Name}", nameof(config))
        };
    }
}
