using UnityEngine;

public class EnvironmentProfile : MonoBehaviour
{
    [Header("天空盒")]
    public Material skyboxMaterial;

    [Header("環境光來源")]
    public UnityEngine.Rendering.AmbientMode ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
    // Trilight = Gradient(Sky/Equator/Ground三色);Flat = Color(單色)

    [Header("Gradient 模式用(三色)")]
    public Color ambientSky = Color.gray;
    public Color ambientEquator = Color.gray;
    public Color ambientGround = Color.gray;

    [Header("Color 模式用(單色)")]
    public Color ambientFlat = Color.gray;

    [Header("霧")]
    public bool fogEnabled = false;
    public Color fogColor = Color.gray;
    public FogMode fogMode = FogMode.Exponential;
    public float fogDensity = 0.001f;
    public float fogStartDistance = 0f;      // Linear 用
    public float fogEndDistance = 300f;      // Linear 用

    // 切到這組時呼叫,套用整套環境
    public void Apply()
    {
        if (skyboxMaterial != null)
            RenderSettings.skybox = skyboxMaterial;

        RenderSettings.ambientMode = ambientMode;
        if (ambientMode == UnityEngine.Rendering.AmbientMode.Flat)
        {
            RenderSettings.ambientLight = ambientFlat;
        }
        else // Trilight / Gradient
        {
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
        }

        RenderSettings.fog = fogEnabled;
        if (fogEnabled)
        {
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = fogMode;
            if (fogMode == FogMode.Linear)
            {
                RenderSettings.fogStartDistance = fogStartDistance;
                RenderSettings.fogEndDistance = fogEndDistance;
            }
            else // Exponential / ExponentialSquared
            {
                RenderSettings.fogDensity = fogDensity;
            }
        }

        DynamicGI.UpdateEnvironment(); // 讓環境光即時生效
    }
}