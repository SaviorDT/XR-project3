using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class SpiralCometTrail : MonoBehaviour
{
    [Header("Spiral Path")]
    public float radius = 1f;
    public float maxDistance = 5f;
    public float forwardSpeed = 2f;
    public float rotateSpeed = 360f;

    [Header("Noise")]
    public float noiseStrength = 0.15f;
    public float noiseSpeed = 3f;

    [Header("Trail")]
    public float trailTime = 1.5f;
    public float trailStartWidth = 0.03f;
    public float trailEndWidth = 0f;

    [Header("End Fade")]
    public float fadeDuration = 0.6f;
    public float fadeForwardSpeedRatio = 0.3f;

    private TrailRenderer trail;
    private float time;
    private Vector3 startLocalPos;
    private float randomOffset;

    private bool fading = false;
    private float fadeTimer = 0f;

    void Awake()
    {
        trail = GetComponent<TrailRenderer>();

        trail.time = trailTime;
        trail.startWidth = trailStartWidth;
        trail.endWidth = trailEndWidth;
        trail.emitting = true;

        startLocalPos = transform.localPosition;
        randomOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (fading)
        {
            UpdateFadeOut();
            return;
        }

        time += Time.deltaTime;

        float currentDistance = time * forwardSpeed;

        if (currentDistance >= maxDistance)
        {
            StartFadeOut();
            return;
        }

        float angle = time * rotateSpeed * Mathf.Deg2Rad;

        float noiseX =
            Mathf.PerlinNoise(randomOffset, time * noiseSpeed) - 0.5f;

        float noiseY =
            Mathf.PerlinNoise(randomOffset + 10f, time * noiseSpeed) - 0.5f;

        Vector3 spiralOffset = new Vector3(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(angle) * radius,
            currentDistance
        );

        Vector3 noiseOffset = new Vector3(
            noiseX,
            noiseY,
            0f
        ) * noiseStrength;

        transform.localPosition = startLocalPos + spiralOffset + noiseOffset;

        Vector3 localMoveDir = new Vector3(
            -Mathf.Sin(angle),
            Mathf.Cos(angle),
            forwardSpeed
        ).normalized;

        transform.localRotation = Quaternion.LookRotation(
            localMoveDir,
            Vector3.up
        );
    }

    private void StartFadeOut()
    {
        fading = true;
        fadeTimer = 0f;
        trail.emitting = true;
    }

    private void UpdateFadeOut()
    {
        fadeTimer += Time.deltaTime;
        time += Time.deltaTime;

        float t = Mathf.Clamp01(fadeTimer / fadeDuration);
        float fade = 1f - t;

        trail.startWidth = trailStartWidth * fade;
        trail.endWidth = trailEndWidth * fade;

        float currentDistance = maxDistance + fadeTimer * forwardSpeed * fadeForwardSpeedRatio;
        float angle = time * rotateSpeed * Mathf.Deg2Rad;

        float noiseX =
            Mathf.PerlinNoise(randomOffset, time * noiseSpeed) - 0.5f;

        float noiseY =
            Mathf.PerlinNoise(randomOffset + 10f, time * noiseSpeed) - 0.5f;

        Vector3 spiralOffset = new Vector3(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(angle) * radius,
            currentDistance
        );

        Vector3 noiseOffset = new Vector3(
            noiseX,
            noiseY,
            0f
        ) * noiseStrength * fade;

        transform.localPosition = startLocalPos + spiralOffset + noiseOffset;

        Vector3 localMoveDir = new Vector3(
            -Mathf.Sin(angle),
            Mathf.Cos(angle),
            forwardSpeed
        ).normalized;

        transform.localRotation = Quaternion.LookRotation(
            localMoveDir,
            Vector3.up
        );

        if (fadeTimer >= fadeDuration)
        {
            trail.emitting = false;
            Destroy(gameObject, trail.time + 0.1f);
        }
    }
}