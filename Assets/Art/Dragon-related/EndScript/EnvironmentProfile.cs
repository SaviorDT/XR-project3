using UnityEngine;

public class EnvironmentProfile : MonoBehaviour
{
    [Header("天空盒")]
    public Material skyboxMaterial;

    [Header("環境光來源")]
    public UnityEngine.Rendering.AmbientMode ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;

    [Header("Gradient 模式用(三色)")]
    [ColorUsage(true, true)] public Color ambientSky = Color.gray;
    [ColorUsage(true, true)] public Color ambientEquator = Color.gray;
    [ColorUsage(true, true)] public Color ambientGround = Color.gray;

    [Header("Color 模式用(單色)")]
    [ColorUsage(true, true)] public Color ambientFlat = Color.gray;

    [Header("主光源 Directional Light 開關")]
    public GameObject directionalLightObject;       // 拖整個 Directional Light 物件
    public Light directionalLight;                  // 拖同一盞的 Light 元件(給 Sun Source 用)
    public bool directionalLightEnabled = true;     // 這組要不要開

    [Header("霧")]
    public bool fogEnabled = false;
    public Color fogColor = Color.gray;
    public FogMode fogMode = FogMode.Exponential;
    public float fogDensity = 0.001f;
    public float fogStartDistance = 0f;
    public float fogEndDistance = 300f;

    public void Apply()
    {
        if (skyboxMaterial != null)
            RenderSettings.skybox = skyboxMaterial;

        RenderSettings.ambientMode = ambientMode;
        if (ambientMode == UnityEngine.Rendering.AmbientMode.Flat)
        {
            RenderSettings.ambientLight = ambientFlat;
        }
        else
        {
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
        }

        // 控制主光源:同時處理 物件開關 + Sun Source 引用
        if (directionalLightObject != null)
            directionalLightObject.SetActive(directionalLightEnabled);

        // 關鍵:Sun Source 也要斷開,否則關了還影響 skybox
        RenderSettings.sun = directionalLightEnabled ? directionalLight : null;

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
            else
            {
                RenderSettings.fogDensity = fogDensity;
            }
        }

        DynamicGI.UpdateEnvironment();
    }
}