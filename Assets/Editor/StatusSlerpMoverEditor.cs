using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StatusSlerpMover))]
public class StatusSlerpMoverEditor : Editor
{
    private void OnSceneGUI()
    {
        StatusSlerpMover mover = (StatusSlerpMover)target;

        if (mover.statusTargets == null)
            return;

        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        for (int i = 0; i < mover.statusTargets.Length; i++)
        {
            var targetData = mover.statusTargets[i];

            Vector3 position = targetData.position;
            Quaternion rotation = Quaternion.Euler(targetData.eulerAngle);

            DrawTargetLabel(targetData);

            EditorGUI.BeginChangeCheck();

            Vector3 newPosition =
                Handles.PositionHandle(
                    position,
                    rotation);

            Quaternion newRotation =
                Handles.RotationHandle(
                    rotation,
                    newPosition);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(
                    mover,
                    $"Modify Status {targetData.status}");

                targetData.position = newPosition;
                targetData.eulerAngle = newRotation.eulerAngles;

                EditorUtility.SetDirty(mover);
            }

            DrawTargetGizmos(targetData);
        }
    }

    private void DrawTargetLabel(
        StatusSlerpMover.StatusTarget targetData)
    {
        GUIStyle style = new GUIStyle();

        style.normal.textColor = Color.white;
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 14;

        Handles.Label(
            targetData.position + Vector3.up * 0.3f,
            $"Status {targetData.status}",
            style);
    }

    private void DrawTargetGizmos(
        StatusSlerpMover.StatusTarget targetData)
    {
        Quaternion rot =
            Quaternion.Euler(targetData.eulerAngle);

        float sphereRadius = 0.15f;
        float axisLength = 0.75f;

        Handles.color = Color.yellow;

        Handles.SphereHandleCap(
            0,
            targetData.position,
            Quaternion.identity,
            sphereRadius,
            EventType.Repaint);

        Vector3 forward =
            rot * Vector3.forward * axisLength;

        Vector3 up =
            rot * Vector3.up * axisLength;

        Vector3 right =
            rot * Vector3.right * axisLength;

        Handles.color = Color.blue;
        Handles.DrawLine(
            targetData.position,
            targetData.position + forward);

        Handles.color = Color.green;
        Handles.DrawLine(
            targetData.position,
            targetData.position + up);

        Handles.color = Color.red;
        Handles.DrawLine(
            targetData.position,
            targetData.position + right);

        Handles.ArrowHandleCap(
            0,
            targetData.position + forward,
            Quaternion.LookRotation(forward),
            0.2f,
            EventType.Repaint);
    }
}