using UnityEngine;

public class ManualHUDRenderer : MonoBehaviour
{
    public Camera hudRenderCamera;
    public MonoBehaviour[] updateBeforeRender;

    void LateUpdate()
    {
        for (int i = 0; i < updateBeforeRender.Length; i++)
        {
            if (updateBeforeRender[i] is IManualHUDUpdate updater)
                updater.ManualHUDUpdate();
        }

        if (hudRenderCamera != null)
            hudRenderCamera.Render();
    }
}

public interface IManualHUDUpdate
{
    void ManualHUDUpdate();
}