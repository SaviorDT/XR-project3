using UnityEngine;

public class ManualHUDRenderer : MonoBehaviour
{
    public Camera hudRenderCamera;

    void LateUpdate()
    {
        if (hudRenderCamera != null)
            hudRenderCamera.Render();
    }
}