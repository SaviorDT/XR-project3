using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CylindricalCanvasSurface : MonoBehaviour
{
    [Header("Render Texture")]
    public RenderTexture sourceTexture;

    [Header("Shape")]
    public float radius = 1.0f;
    public float totalAngle = 70f;
    public float verticalSize = 1.2f;

    [Header("Resolution")]
    public int xSegments = 64;
    public int ySegments = 1;

    [Header("Material")]
    public Shader shader;

    private Material material;

    void Start()
    {
        Generate();
    }

    public void Generate()
    {
        if (sourceTexture == null)
        {
            Debug.LogError("Source Texture is null.");
            return;
        }

        Mesh mesh = new Mesh();
        mesh.name = "Cylindrical Canvas Surface";

        Vector3[] vertices = new Vector3[(xSegments + 1) * (ySegments + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[xSegments * ySegments * 6];

        int index = 0;

        for (int y = 0; y <= ySegments; y++)
        {
            float v = (float)y / ySegments;
            float yPos = Mathf.Lerp(-verticalSize / 2f, verticalSize / 2f, v);

            for (int x = 0; x <= xSegments; x++)
            {
                float u = (float)x / xSegments;
                float angle = Mathf.Lerp(-totalAngle / 2f, totalAngle / 2f, u);

                Quaternion rot = Quaternion.Euler(0f, angle, 0f);
                Vector3 pos = rot * Vector3.forward * radius;

                vertices[index] = new Vector3(pos.x, yPos, pos.z);
                uvs[index] = new Vector2(u, v);

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

        GetComponent<MeshFilter>().mesh = mesh;

        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");

        material = new Material(shader);
        material.mainTexture = sourceTexture;

        GetComponent<MeshRenderer>().material = material;
    }
}