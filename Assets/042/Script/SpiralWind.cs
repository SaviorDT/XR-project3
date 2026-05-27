using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class SpiralWindParticle : MonoBehaviour
{
    public float radius = 1f;
    public float height = 5f;
    public float turns = 4f;
    public int particleCount = 120;

    public float rotateSpeed = 180f;
    public float particleSize = 0.15f;
    public Color particleColor = Color.white;

    private ParticleSystem ps;
    private ParticleSystem.Particle[] particles;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();

        var main = ps.main;
        main.maxParticles = particleCount;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.playOnAwake = true;
        main.loop = true;
        main.startLifetime = 999f;
        main.startSpeed = 0f;
        main.startSize = particleSize;

        var emission = ps.emission;
        emission.enabled = false;

        particles = new ParticleSystem.Particle[particleCount];

        for (int i = 0; i < particleCount; i++)
        {
            particles[i].startLifetime = 999f;
            particles[i].remainingLifetime = 999f;
            particles[i].startSize = particleSize;
            particles[i].startColor = particleColor;
        }

        ps.SetParticles(particles, particleCount);
        ps.Play();
    }

    void LateUpdate()
    {
        if (particles == null) return;

        float timeAngle = Time.time * rotateSpeed * Mathf.Deg2Rad;

        for (int i = 0; i < particleCount; i++)
        {
            float t = i / (float)(particleCount - 1);
            float angle = t * turns * Mathf.PI * 2f + timeAngle;

            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            float y = t * height;

            particles[i].position = new Vector3(x, y, z);
            particles[i].remainingLifetime = 999f;
        }

        ps.SetParticles(particles, particleCount);
    }
}