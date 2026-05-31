using UnityEngine;

public class CurvedCanvasSlices : MonoBehaviour
{
    [Header("Render Texture")]
    public RenderTexture sourceTexture;

    [Header("Shape")]
    public int sliceCount = 32;

    [Tooltip("弧面半徑")]
    public float radius = 1.0f;

    [Tooltip("總彎曲角度")]
    public float totalAngle = 70f;

    [Tooltip("整體高度偏移")]
    public float height = 0f;

    [Tooltip("弧面高度")]
    public float verticalSize = 1.2f;

    [Header("Material")]
    public Shader shader;

    private Material sharedMaterial;

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
        ClearChildren();

        if (sourceTexture == null)
        {
            Debug.LogError("Source Texture is null.");
            return;
        }

        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");

        sharedMaterial = new Material(shader);
        sharedMaterial.mainTexture = sourceTexture;

        for (int i = 0; i < sliceCount; i++)
        {
            CreateSlice(i);
        }
    }

    void CreateSlice(int index)
    {
        GameObject slice = new GameObject("Slice_" + index);

        slice.transform.SetParent(transform, false);

        MeshFilter mf = slice.AddComponent<MeshFilter>();
        MeshRenderer mr = slice.AddComponent<MeshRenderer>();

        mr.material = sharedMaterial;

        float u0 = (float)index / sliceCount;
        float u1 = (float)(index + 1) / sliceCount;

        mf.mesh = CreateSliceMesh(u0, u1);

        float t = ((float)index + 0.5f) / sliceCount;

        float angle = Mathf.Lerp(
            -totalAngle / 2f,
            totalAngle / 2f,
            t
        );

        Quaternion rot = Quaternion.Euler(0f, angle, 0f);

        Vector3 pos = rot * Vector3.forward * radius;

        slice.transform.localPosition =
            pos + Vector3.up * height;

        slice.transform.localRotation = rot;
    }

    Mesh CreateSliceMesh(float u0, float u1)
    {
        Mesh mesh = new Mesh();

        float anglePerSlice = totalAngle / sliceCount;

        float arcWidth =
            2f *
            Mathf.PI *
            radius *
            (anglePerSlice / 360f);

        Vector3[] vertices =
        {
            new Vector3(-arcWidth / 2f, -verticalSize / 2f, 0f),
            new Vector3( arcWidth / 2f, -verticalSize / 2f, 0f),
            new Vector3(-arcWidth / 2f,  verticalSize / 2f, 0f),
            new Vector3( arcWidth / 2f,  verticalSize / 2f, 0f)
        };

        Vector2[] uv =
        {
            new Vector2(u0, 0f),
            new Vector2(u1, 0f),
            new Vector2(u0, 1f),
            new Vector2(u1, 1f)
        };

        int[] triangles =
        {
            0, 2, 1,
            1, 2, 3
        };

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    void ClearChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(transform.GetChild(i).gameObject);
            else
                DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
}