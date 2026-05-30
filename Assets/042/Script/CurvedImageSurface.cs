using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CurvedImageSurface : MonoBehaviour
{
    public Material material;

    public float radius = 3f;
    public float widthAngle = 120f;
    public float heightAngle = 60f;

    public int xSegments = 32;
    public int ySegments = 16;

    void Start()
    {
        GenerateMesh();

        if (material != null)
            GetComponent<MeshRenderer>().material = material;
    }

    void GenerateMesh()
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[(xSegments + 1) * (ySegments + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[xSegments * ySegments * 6];

        int index = 0;

        for (int y = 0; y <= ySegments; y++)
        {
            float v = (float)y / ySegments;
            float pitch = Mathf.Lerp(heightAngle / 2f, -heightAngle / 2f, v);

            for (int x = 0; x <= xSegments; x++)
            {
                float u = (float)x / xSegments;
                float yaw = Mathf.Lerp(-widthAngle / 2f, widthAngle / 2f, u);

                Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
                vertices[index] = rot * Vector3.forward * radius;

                uv[index] = new Vector2(u, 1f - v);
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
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
    }
}