using UnityEngine;

public class HemisphereUI : MonoBehaviour
{
    public Transform cameraTarget;
    public GameObject buttonPrefab;

    public int rows = 4;
    public int columns = 7;

    public float radius = 2.5f;
    public float horizontalAngle = 120f;
    public float verticalAngle = 60f;

    void Start()
    {
        GenerateUI();
    }
    void Update()
    {
        if (cameraTarget == null) return;

        transform.position = cameraTarget.position + cameraTarget.forward * 2.5f;
        transform.rotation = Quaternion.Euler(0, cameraTarget.eulerAngles.y, 0);
    }
    void GenerateUI()
    {
        for (int y = 0; y < rows; y++)
        {
            float v = rows == 1 ? 0.5f : (float)y / (rows - 1);
            float pitch = Mathf.Lerp(verticalAngle / 2f, -verticalAngle / 2f, v);

            for (int x = 0; x < columns; x++)
            {
                float u = columns == 1 ? 0.5f : (float)x / (columns - 1);
                float yaw = Mathf.Lerp(-horizontalAngle / 2f, horizontalAngle / 2f, u);

                Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
                Vector3 localPos = rot * Vector3.forward * radius;

                GameObject obj = Instantiate(buttonPrefab, transform);

                RectTransform rect = obj.GetComponent<RectTransform>();
                rect.anchoredPosition3D = localPos;
                rect.localScale = Vector3.one;

                rect.LookAt(cameraTarget.transform);
                //rect.Rotate(0, 180f, 0);
            }
        }
    }
}