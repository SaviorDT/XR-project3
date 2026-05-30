using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class FitCameraToWorldCanvas : MonoBehaviour
{
    public RectTransform targetCanvas;

    public float padding = 1.05f;
    public float distance = 10f;

    private Camera cam;

    void OnEnable()
    {
        cam = GetComponent<Camera>();
        Fit();
    }

    void LateUpdate()
    {
        Fit();
    }

    void Fit()
    {
        if (targetCanvas == null) return;

        if (cam == null)
            cam = GetComponent<Camera>();

        cam.orthographic = true;

        Vector3 center = targetCanvas.TransformPoint(targetCanvas.rect.center);

        transform.position = center - targetCanvas.forward * distance;
        transform.rotation = Quaternion.LookRotation(targetCanvas.forward, targetCanvas.up);

        float worldHeight = targetCanvas.rect.height * targetCanvas.lossyScale.y;
        float worldWidth = targetCanvas.rect.width * targetCanvas.lossyScale.x;

        float canvasAspect = worldWidth / worldHeight;
        float cameraAspect = cam.aspect;

        if (canvasAspect > cameraAspect)
            cam.orthographicSize = (worldWidth / cameraAspect) * 0.5f * padding;
        else
            cam.orthographicSize = worldHeight * 0.5f * padding;
    }
}