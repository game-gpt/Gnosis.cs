using System.Numerics;

namespace Gnosis.Graphic.Sky;

public sealed class SkySettings : ISkySettings
{
    #region 属性

    public bool EnableAtmosphere { get; set; }
    public bool EnableSkybox { get; set; }
    public float SkyDistance { get; set; }
    public Vector3 SunDirection { get; set; }
    public float SunSize { get; set; }
    public Vector3 SunColor { get; set; }
    public float StarIntensity { get; set; }

    #endregion

    #region 构造函数

    public SkySettings()
    {
        EnableAtmosphere = true;
        EnableSkybox = false;
        SkyDistance = 1000.0f;
        SunDirection = Vector3.Normalize(new Vector3(0.3f, 0.8f, 0.2f));
        SunSize = 0.05f;
        SunColor = new Vector3(1.0f, 0.95f, 0.8f);
        StarIntensity = 0.0f;
    }

    #endregion
}
