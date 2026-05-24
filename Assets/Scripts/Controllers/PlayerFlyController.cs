using System;
using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(Rigidbody))]
public class PlayerFlyController : MonoBehaviour
{
    [SerializeField] private float VelocitySteeringRatio = 1.0f;
    [SerializeField] private float CorrectPitchRatio = 0.3f;
    [SerializeField] private float PlayerRollThreshold = 5.0f;
    [SerializeField] private float RollSteeringRatio = 1.0f;
    [Tooltip("玩家的平移距離超過此閾值才會被視為俯衝或抬頭")]
    [SerializeField] private float PlayerPitchThreshold = 0.1f;
    [SerializeField] private float PlayerControllerRotateToPitchRatio = 1.0f;
    [SerializeField] private float Gravity = 50f, ReducedGravityRatio = 0.75f, StallSpeed = 5.0f;
    [SerializeField] private float WindForce = 1.0f;
    [SerializeField] private float DownToForwardRatio = 2.0f, DownToForwardLossRatio = 0.0f;
    [SerializeField] private float VelocityToUpRatio = 0.8f, VelocityToUpLossRatio = -2f;
    [Tooltip("1秒後，玩家的速度會有多少比例轉向當前的飛行方向")]
    [SerializeField] private float SteeringSpeed = 1.5f;
    [SerializeField] private Vector3 WindResistance = new(0.5f, 0.5f, 0.5f);
    // 1. 轉向目前速度方向
    // 1. 將玩家歪頭套用到轉向
    // 1. 計算俯仰角
    // 2. 套用重力加速度
    // 3. 套用風力加速度
    // 4. 將當前往下的速度轉換為往前的速度
    // 5. 將當前往前的速度轉換為往上的速度
    // 1. 將速度轉向前面
    // 6. 套用風阻

    // 以下為計算用變數
    [SerializeField] private Vector3 Velocity = Vector3.zero;
    private float PlayerRoll = 0.0f;
    private enum EPitchState { Up, Neutral, Down }
    private EPitchState PlayerPitchState = EPitchState.Neutral;
    private float PlayerControllerRotateBias = 0.0f;
    private float PlayerControllerRotateY = 0.0f;
    [SerializeField] private float DiveAngle = -70.0f, ClimbAngle = 70.0f;
    private Vector3 WindVelocity = Vector3.zero;
    private InputDevice HeadDevice;
    private InputDevice LeftHandDevice;
    private InputDevice RightHandDevice;
    private Rigidbody PlayerRigidbody;
    
    void Start()
    {
        PlayerRigidbody = GetComponent<Rigidbody>();
        if (PlayerRigidbody != null)
        {
            PlayerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void Update()
    {
        if (!HeadDevice.isValid)
        {
            HeadDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        }

        if (!LeftHandDevice.isValid)
        {
            LeftHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        }

        if (!RightHandDevice.isValid)
        {
            RightHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        }

        if (HeadDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out var headRotation))
        {
            var roll = headRotation.eulerAngles.z;
            if (roll > 180.0f)
            {
                roll -= 360.0f;
            }

            PlayerRoll = roll;
        }

        if (HeadDevice.TryGetFeatureValue(CommonUsages.devicePosition, out var headPosition))
        {
            // PlayerControllerRotateY = headPosition.x * 100.0f;
            if (Mathf.Abs(headPosition.z) > PlayerPitchThreshold)
            {
                PlayerPitchState = headPosition.z > 0.0f ? EPitchState.Down : EPitchState.Up;
            }
            else
            {
                PlayerPitchState = EPitchState.Neutral;
            }
        }

        if (LeftHandDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out var leftRotation) &&
            RightHandDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out var rightRotation))
        {
            PlayerControllerRotateY = (leftRotation.y + rightRotation.y) * 0.5f * Mathf.Rad2Deg;
        }
    }
    
    void FixedUpdate()
    {
        // 轉向
        Vector3 horizontalVelocity = new(Velocity.x, 0.0f, Velocity.z);
        Quaternion targetRotation = PlayerRigidbody.rotation;
        if (horizontalVelocity.sqrMagnitude > 0.0001f)
        {
            // 轉向前進方向
            Vector3 horizontalForward = new Vector3(transform.forward.x, 0.0f, transform.forward.z).normalized;
            float thetaY = Vector3.SignedAngle(horizontalForward, horizontalVelocity.normalized, Vector3.up);
            targetRotation = Quaternion.AngleAxis(thetaY * VelocitySteeringRatio, Vector3.up) * targetRotation;
        }

        // 玩家歪頭轉向
        if (Mathf.Abs(PlayerRoll) > PlayerRollThreshold)
        {
            targetRotation = Quaternion.AngleAxis(-PlayerRoll * RollSteeringRatio, Vector3.up) * targetRotation;
            Debug.Log($"PlayerRoll: {PlayerRoll}, PlayerControllerRotateY: {PlayerControllerRotateY}");
        }

        // 改平（保留 yaw）
        Vector3 flatForward = Vector3.ProjectOnPlane(targetRotation * Vector3.forward, Vector3.up);
        if (flatForward.sqrMagnitude > 0.0001f)
        {
            Quaternion uprightRotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
            targetRotation = Quaternion.Slerp(targetRotation, uprightRotation, CorrectPitchRatio);
        }
        Quaternion deltaRotation = targetRotation * Quaternion.Inverse(PlayerRigidbody.rotation);
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 0.0001f)
        {
            PlayerRigidbody.angularVelocity = axis * (angle * Mathf.Deg2Rad);
        }
        else
        {
            PlayerRigidbody.angularVelocity = Vector3.zero;
        }

        // 計算俯仰角
        Vector3 forwardHorizontal = new(transform.forward.x, 0.0f, transform.forward.z);
        float pitch = 0.0f;
        if (forwardHorizontal.sqrMagnitude > 0.0001f)
        {
            pitch = Vector3.SignedAngle(forwardHorizontal.normalized, transform.forward.normalized, transform.right);
        }
        if (PlayerPitchState == EPitchState.Up)
        {
            pitch += ClimbAngle;
        }
        else if (PlayerPitchState == EPitchState.Down)
        {
            pitch += DiveAngle;
        }
        pitch += (PlayerControllerRotateY - PlayerControllerRotateBias) * PlayerControllerRotateToPitchRatio;
        pitch = Mathf.Clamp(pitch, -89.9f, 89.9f) * Mathf.Deg2Rad;

        // 重力加速度
        if (horizontalVelocity.magnitude > StallSpeed)
        {
            Velocity += Gravity * (1 - ReducedGravityRatio * Mathf.Cos(pitch) * Mathf.Cos(pitch)) * Time.fixedDeltaTime * Vector3.down;
}
        else
        {
            // 和上面差不多，當速度低於失速速度時，重力被抵銷的比例會更少
            Velocity += Gravity * (1 - ReducedGravityRatio * (horizontalVelocity.magnitude / StallSpeed)
                                        * Mathf.Cos(pitch) * Mathf.Cos(pitch)) * Time.fixedDeltaTime * Vector3.down;
        }
        
        // 風力加速度
        if (WindVelocity.sqrMagnitude > 0.0001f)
        {
            Velocity += WindForce * Time.fixedDeltaTime * (WindVelocity - Vector3.Project(Velocity, WindVelocity.normalized));
        }

        float ReducedDownSpeed = 0.0f;
        // 下降轉往前
        if (Velocity.y < 0.0f)
        {
            ReducedDownSpeed = -Velocity.y * DownToForwardRatio * Mathf.Cos(pitch) * Mathf.Cos(pitch) * Time.fixedDeltaTime;
            Velocity.y += ReducedDownSpeed;
            Velocity += ReducedDownSpeed * (1 - DownToForwardLossRatio) * transform.forward;
        }

        // 相對速度轉往上
        Vector3 RelativeVelocity = Velocity - WindVelocity;
        float ReducedForwardSpeed = -VelocityToUpRatio * 
                                    new Vector3(RelativeVelocity.x, 0, RelativeVelocity.z).magnitude * 
                                    Mathf.Sin(pitch) * Mathf.Sin(pitch) *
                                    Time.fixedDeltaTime;
        
        Velocity += ReducedForwardSpeed * RelativeVelocity.normalized;
        Velocity.y += ReducedForwardSpeed * (1 - VelocityToUpLossRatio) * (pitch > 0 ? -1 : 1);

        // 速度轉向前面
        Vector3 ReducedSidewaysVelocity = SteeringSpeed * Time.fixedDeltaTime * -new Vector3(Velocity.x, 0.0f, Velocity.z);
        Velocity += ReducedSidewaysVelocity;
        Velocity -= Vector3.Project(ReducedSidewaysVelocity, transform.forward);

        // 風阻
        Velocity = Vector3.Scale(Velocity, WindResistance * Time.fixedDeltaTime + Vector3.one * (1 - Time.fixedDeltaTime));

        // 移動
        PlayerRigidbody.linearVelocity = Velocity;

        // Debug.Log($"Velocity: {Velocity}, ReducedDownSpeed: {ReducedDownSpeed}, ReducedForwardSpeed: {ReducedForwardSpeed}");
        // Debug.Log($"Pitch: {pitch * Mathf.Rad2Deg}, PlayerPitchState: {PlayerPitchState}, PlayerControllerRotateY: {PlayerControllerRotateY}, PlayerControllerRotateBias: {PlayerControllerRotateBias}");
    }

    public void SetWindVelocity(Vector3 velocity)
    {
        WindVelocity += velocity;
    }

    public void SetPlayerControllerRotateBias()
    {
        PlayerControllerRotateBias = PlayerControllerRotateY;
    }
}
