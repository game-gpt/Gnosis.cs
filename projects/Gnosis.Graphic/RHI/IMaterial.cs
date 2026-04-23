namespace Gnosis.Graphic.RHI;

/// <summary>
///     材质接口，管理着色器参数
/// </summary>
public interface IMaterial
{
    /// <summary>
    ///     关联的着色器程序
    /// </summary>
    IShaderProgram Shader { get; }

    /// <summary>
    ///     获取参数值
    /// </summary>
    T GetParameter<T>(string name) where T : struct;

    /// <summary>
    ///     设置参数值
    /// </summary>
    void SetParameter<T>(string name, T value) where T : struct;
}
