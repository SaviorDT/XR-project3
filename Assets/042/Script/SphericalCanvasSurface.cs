using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SphericalCanvasSurface : MonoBehaviour
{
    [Header("Render Texture")]
    public RenderTexture sourceTexture;

    [Header("Sphere Patch")]
    public float radius = 1.2f;

    [Tooltip("左右展開角度")]
    public float horizontalAngle = 90f;

    [Tooltip("上下展開角度")]
    public float verticalAngle = 55f;

    [Header("Resolution")]
    public int xSegments = 64;
    public int ySegments = 32;

    [Header("Material")]
    public Shader shader;

    private Material material;

    void Start()
    {
        Generate();
    }

    void OnValidate()
    {
        if (Application.isPlaying)
            Generate();
    }

    public void Generate()
    {
        if (sourceTexture == null)
        {
            Debug.LogWarning("Source Texture is null.");
            return;
        }

        MeshFilter mf = GetComponent<MeshFilter>();
        MeshRenderer mr = GetComponent<MeshRenderer>();

        Mesh mesh = new Mesh();
        mesh.name = "Spherical Canvas Surface";

        Vector3[] vertices = new Vector3[(xSegments + 1) * (ySegments + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[xSegments * ySegments * 6];

        int index = 0;

        for (int y = 0; y <= ySegments; y++)
        {
            float v = (float)y / ySegments;

            float pitch = Mathf.Lerp(
                verticalAngle / 2f,
                -verticalAngle / 2f,
                v
            );

            for (int x = 0; x <= xSegments; x++)
            {
                float u = (float)x / xSegments;

                float yaw = Mathf.Lerp(
                    -horizontalAngle / 2f,
                    horizontalAngle / 2f,
                    u
                );

                Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);

                vertices[index] = rot * Vector3.forward * radius;

                uvs[index] = new Vector2(u, 1f - v);

                index++;
            }
        }

        int t = 0;

        for (int y = 0; y < ySegments; y++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int i = y * (xSegments + 1) + x;

                triangles[t++] = i;
                triangles[t++] = i + xSegments + 1;
                triangles[t++] = i + 1;

                triangles[t++] = i + 1;
                triangles[t++] = i + xSegments + 1;
                triangles[t++] = i + xSegments + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        mf.mesh = mesh;

        if (shader == null)
            shader = Shader.Find("Unlit/Texture");

        if (material == null)
            material = new Material(shader);

        material.mainTexture = sourceTexture;
        mr.material = material;
    }
}