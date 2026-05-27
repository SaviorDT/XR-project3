using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class WindParticleFollowParent : MonoBehaviour
{
    [Header("Reference")]
    public Transform windRoot;

    [Header("Local Offset Under Root")]
    public Vector3 localPositionOffset = Vector3.zero;
    public Vector3 localEulerOffset = Vector3.zero;
    public Vector3 localScaleMultiplier = new Vector3(1f, 1f, 0.1f);

    [Header("Particle Density / Size")]
    public float baseRateOverTime = 80f;
    public float baseStartSize = 0.08f;

    [Header("Wind Speed")]
    public float baseWindSpeed = 5f;

    [Header("Noise")]
    public bool scaleNoiseWithRoot = true;
    public float baseNoiseStrength = 0.2f;
    public float baseNoiseFrequency = 0.6f;

    [Header("Trail")]
    public float baseTrailWidth = 0.03f;
    public AnimationCurve trailWidthCurve = new AnimationCurve(
        new Keyframe(0f, 0.02f),
        new Keyframe(0.2f, 0.03f),
        new Keyframe(1f, 0f)
    );

    [Header("Length")]
    public float baseStartLifetime = 1.5f;
    public bool scaleLengthWithRootZ = true;

    private ParticleSystem ps;
    private Vector3 initialLocalScale;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();

        if (windRoot == null && transform.parent != null)
            windRoot = transform.parent;

        initialLocalScale = transform.localScale;

        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
    }

    void LateUpdate()
    {
        if (windRoot == null) return;

        transform.position = windRoot.TransformPoint(localPositionOffset);
        transform.rotation = windRoot.rotation * Quaternion.Euler(localEulerOffset);

        // 不要縮放 ParticleSystem 本體，避免風向被非等比 scale 影響
        transform.localScale = initialLocalScale;

        ApplyParticleSettings();
    }

    private void ApplyParticleSettings()
    {
        Vector3 rootScale = AbsVector(windRoot.lossyScale);
        Vector3 shapeScale = localScaleMultiplier;/*new Vector3(
            rootScale.x * localScaleMultiplier.x,
            rootScale.y * localScaleMultiplier.y,
            rootScale.z * localScaleMultiplier.z
        );*/

        float rootScaleAvg = GetGeometricAverageScale(rootScale);
        float rootScaleXY = (rootScale.x + rootScale.y) * 0.5f;

        var emission = ps.emission;
        emission.rateOverTime = baseRateOverTime * Mathf.Max(1f, rootScaleXY);

        var main = ps.main;
        main.startSize = baseStartSize;

        float lengthScale = scaleLengthWithRootZ
            ? Mathf.Max(0.1f, Mathf.Abs(windRoot.lossyScale.z))
            : 1f;

        main.startLifetime = baseStartLifetime * lengthScale;

        var shape = ps.shape;
        shape.scale = shapeScale;

        var velocity = ps.velocityOverLifetime;
        if (velocity.enabled)
        {
            velocity.space = ParticleSystemSimulationSpace.World;

            Vector3 worldForward =
                (windRoot.rotation * Quaternion.Euler(localEulerOffset)) * Vector3.forward;

            velocity.x = new ParticleSystem.MinMaxCurve(worldForward.x * baseWindSpeed);
            velocity.y = new ParticleSystem.MinMaxCurve(worldForward.y * baseWindSpeed);
            velocity.z = new ParticleSystem.MinMaxCurve(worldForward.z * baseWindSpeed);
        }

        var noise = ps.noise;
        if (noise.enabled && scaleNoiseWithRoot)
        {
            float noiseScale = Mathf.Max(0.15f, rootScaleAvg);

            noise.strength = new ParticleSystem.MinMaxCurve(baseNoiseStrength * noiseScale);
            noise.frequency = baseNoiseFrequency / noiseScale;
        }

        var trails = ps.trails;
        if (trails.enabled)
        {
            trails.widthOverTrail = new ParticleSystem.MinMaxCurve(
                1f,
                trailWidthCurve
            );
        }
    }

    private Vector3 AbsVector(Vector3 v)
    {
        return new Vector3(
            Mathf.Abs(v.x),
            Mathf.Abs(v.y),
            Mathf.Abs(v.z)
        );
    }

    private float GetGeometricAverageScale(Vector3 s)
    {
        return Mathf.Pow(
            Mathf.Max(0.0001f, s.x * s.y * s.z),
            1f / 3f
        );
    }
}