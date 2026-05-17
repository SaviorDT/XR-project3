using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerForceMovement : MonoBehaviour
{
    [Header("References")]
    public Flying flying;
    public Transform cameraTransform;

    [Header("Walk Mode")]
    public float walkSpeed = 5f;
    public float walkTurnSpeed = 120f;

    [Header("Fly Mode")]
    public float boostForce = 25f;
    public float boostDuration = 1f;
    public float boostCooldown = 3f;
    public float flyTurnForce = 8f;


    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float minPitch = -70f;
    public float maxPitch = 70f;

    [Header("Camera Reset")]
    public float cameraResetSpeed = 6f;

    private float cameraPitch = 0f;
    private float cameraYaw = 0f;

    private float nextBoostTime = 0f;

    private Vector3 qForce;
    private Vector3 eForce;

    private bool qPressed;
    private bool ePressed;

    void Awake()
    {
        if (flying == null)
            flying = GetComponent<Flying>();

        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
                cameraTransform = cam.transform;
        }
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (flying.IsFlyingMode)
        {
            HandleFlyMode();
        }
        else
        {
            HandleWalkMode();
        }

        HandleMouseLook();
        HandleCameraResetWhenWalkMode();
    }

    private void HandleWalkMode()
    {
        Rigidbody rb = flying.GetRigidbody();

        Vector3 moveDir = Vector3.zero;

        if (Keyboard.current.wKey.isPressed)
            moveDir += transform.forward;

        if (Keyboard.current.sKey.isPressed)
            moveDir -= transform.forward;

        if (Keyboard.current.qKey.isPressed)
            moveDir -= transform.right;

        if (Keyboard.current.eKey.isPressed)
            moveDir += transform.right;

        if (Keyboard.current.aKey.isPressed)
            transform.Rotate(Vector3.up, -walkTurnSpeed * Time.deltaTime);

        if (Keyboard.current.dKey.isPressed)
            transform.Rotate(Vector3.up, walkTurnSpeed * Time.deltaTime);

        if (moveDir.sqrMagnitude > 0.001f)
        {
            Vector3 displacement = moveDir.normalized * walkSpeed * Time.deltaTime;
            rb.MovePosition(rb.position + displacement);
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TryBoost();
        }
    }

    private void HandleFlyMode()
    {
        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            TryBoost();
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            qForce = -transform.right * flyTurnForce;
            flying.AddForce(qForce);
            qPressed = true;
        }

        if (Keyboard.current.qKey.wasReleasedThisFrame && qPressed)
        {
            flying.RemoveForce(qForce);
            qPressed = false;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            eForce = transform.right * flyTurnForce;
            flying.AddForce(eForce);
            ePressed = true;
        }

        if (Keyboard.current.eKey.wasReleasedThisFrame && ePressed)
        {
            flying.RemoveForce(eForce);
            ePressed = false;
        }
    }

    private void TryBoost()
    {
        if (Time.time < nextBoostTime) return;

        Vector3 force = (transform.forward + transform.up).normalized * boostForce;

        flying.ForceFlyMode();
        StartCoroutine(ApplyBoostForce(force));

        nextBoostTime = Time.time + boostCooldown;
    }

    private IEnumerator ApplyBoostForce(Vector3 force)
    {
        flying.AddForce(force);

        yield return new WaitForSeconds(boostDuration);

        flying.RemoveForce(force);
    }

    private void HandleMouseLook()
    {
        if (Mouse.current == null || cameraTransform == null) return;

        if (!Mouse.current.rightButton.isPressed) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float yawDelta = mouseDelta.x * mouseSensitivity;
        float pitchDelta = mouseDelta.y * mouseSensitivity;

        cameraPitch -= pitchDelta;
        cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);

        if (flying.IsFlyingMode)
        {
            // Fly Mode：
            // 只有 Camera 自己轉，PlayerRoot 完全不受影響
            cameraYaw += yawDelta;

            cameraTransform.localRotation = Quaternion.Euler(
                cameraPitch,
                cameraYaw,
                0f
            );
        }
        else
        {
            transform.Rotate(Vector3.up, yawDelta);

            cameraYaw = 0f;

            cameraTransform.localRotation = Quaternion.Euler(
                cameraPitch,
                0f,
                0f
            );
        }
    }

    private void HandleCameraResetWhenWalkMode()
    {
        if (cameraTransform == null) return;
        if (flying.IsFlyingMode) return;
        if (Mouse.current != null && Mouse.current.rightButton.isPressed) return;

        // 只處理 Camera 的 yaw 差異，pitch 保留
        float currentCameraYaw = cameraYaw;

        if (Mathf.Abs(currentCameraYaw) < 0.1f)
        {
            cameraYaw = 0f;
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            return;
        }

        // 這一幀要把多少 yaw 從 Camera 轉移到 PlayerRoot
        float yawStep = Mathf.Lerp(
            0f,
            currentCameraYaw,
            cameraResetSpeed * Time.deltaTime
        );

        // PlayerRoot 往 Camera 方向轉
        transform.Rotate(Vector3.up, yawStep, Space.World);

        // Camera 反方向扣掉同樣的 yaw，抵銷 PlayerRoot 旋轉造成的畫面變化
        cameraYaw -= yawStep;

        cameraTransform.localRotation = Quaternion.Euler(
            cameraPitch,
            cameraYaw,
            0f
        );
    }
}