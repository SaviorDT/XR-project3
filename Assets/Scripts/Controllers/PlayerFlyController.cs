using System;
using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(Rigidbody))]
public class PlayerFlyController : MonoBehaviour
{
    [SerializeField] private Vector3 FlappingWingForce = new(0.0f, 5.0f, 2.0f);
    [SerializeField] private Vector3 TakeOffVelocity = new(0.0f, 5.0f, 2.0f);
    [SerializeField] private float FlappingWingThreshold = 0.5f;
    [SerializeField] private float VelocitySteeringRatio = 1.0f;
    [SerializeField] private float CorrectPitchRatio = 0.3f;
    [SerializeField] private float PlayerRollThreshold = 5.0f;
    [SerializeField] private float RollSteeringRatio = 3.0f;
    [SerializeField] private float MaxAngularVelocityY = 90.0f;
    [Tooltip("玩家的平移距離超過此閾值才會被視為俯衝或抬頭")]
    [SerializeField] private float PlayerPitchThreshold = 0.1f;
    [SerializeField] private float PlayerControllerRotateToPitchRatio = 1.0f;
    [SerializeField] private float Gravity = 9.8f, ReducedGravityRatio = 0.75f, StallSpeed = 5.0f;
    [SerializeField] private float WindForce = 1.0f;
    [SerializeField] private float DownToForwardRatio = 2.0f, DownToForwardLossRatio = 0.0f;
    [SerializeField] private float VelocityToUpRatio = 0.8f, VelocityToUpLossRatio = -2f;
    [Tooltip("1秒後，玩家的速度會有多少比例轉向當前的飛行方向")]
    [SerializeField] private float SteeringSpeed = 1.5f;
    [SerializeField] private Vector3 WindResistance = new(0.5f, 0.5f, 0.5f);
    [SerializeField] private Transform FixRotationTarget;
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
    [SerializeField] private BoxCollider PlayerCollider;
    private Vector3 CenterPosition = Vector3.zero;
    private float ForwardRotation = 0.0f;
    
    void Start()
    {
        PlayerRigidbody = GetComponent<Rigidbody>();
        if (PlayerCollider == null)
        {
            PlayerCollider = GetComponent<BoxCollider>();
        }
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

        if (CenterPosition == Vector3.zero)
        {
            ResetPlayerPose(false);
        }

        if (HeadDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out var headRotation))
        {
            Quaternion relativeRotation = Quaternion.Inverse(Quaternion.Euler(0.0f, ForwardRotation, 0.0f)) * headRotation;
            var roll = relativeRotation.eulerAngles.z;
            if (roll > 180.0f)
            {
                roll -= 360.0f;
            }

            PlayerRoll = roll;
        }

        if (HeadDevice.TryGetFeatureValue(CommonUsages.devicePosition, out var headPosition))
        {
            Vector3 relativePosition = Quaternion.Inverse(Quaternion.Euler(0.0f, ForwardRotation, 0.0f)) * (headPosition - CenterPosition);
            // PlayerControllerRotateY = relativePosition.x * 100.0f;
            if (Mathf.Abs(relativePosition.z) > PlayerPitchThreshold)
            {
                PlayerPitchState = relativePosition.z > 0.0f ? EPitchState.Down : EPitchState.Up;
            }
            else
            {
                PlayerPitchState = EPitchState.Neutral;
            }
        }

        if (LeftHandDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out var leftRotation) &&
            RightHandDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out var rightRotation))
        {
            Quaternion relativeRotation = Quaternion.Inverse(leftRotation) * rightRotation;
            float relativeYaw = Mathf.DeltaAngle(0.0f, relativeRotation.eulerAngles.y);
            PlayerControllerRotateY = relativeYaw * 0.5f;
            // Debug.Log($"Relative Yaw: {relativeYaw}, PlayerControllerRotateY: {PlayerControllerRotateY}");
        }
    }
    
    void FixedUpdate()
    {
        // 著地時不飛行
        float flappingSpeed = GetFlappingWingSpeed();
        if (IsGrounded())
        {
            // 起飛
            if (flappingSpeed > FlappingWingThreshold)
            {
                ResetPlayerPose(true);
                Velocity = transform.TransformDirection(TakeOffVelocity);
            }
            else {
                Velocity = Vector3.zero;
            }
            PlayerRigidbody.linearVelocity = Velocity;
            PlayerRigidbody.angularVelocity = Vector3.zero;
            return;
        }

        // 空中揮翅
        if (flappingSpeed > FlappingWingThreshold)
        {
            Velocity += flappingSpeed * transform.TransformDirection(FlappingWingForce);
        }

        // 轉向
        Vector3 horizontalVelocity = new(Velocity.x, 0.0f, Velocity.z);
        Quaternion targetRotation = PlayerRigidbody.rotation;
        float yawDelta = 0.0f;
        if (horizontalVelocity.sqrMagnitude > 0.0001f)
        {
            // 轉向前進方向
            Vector3 horizontalForward = new Vector3(transform.forward.x, 0.0f, transform.forward.z).normalized;
            float thetaY = Vector3.SignedAngle(horizontalForward, horizontalVelocity.normalized, Vector3.up);
            yawDelta += thetaY * VelocitySteeringRatio;
        }

        // 玩家歪頭轉向
        if (Mathf.Abs(PlayerRoll) > PlayerRollThreshold)
        {
            yawDelta += -PlayerRoll * RollSteeringRatio;
            // Debug.Log($"PlayerRoll: {PlayerRoll}, PlayerControllerRotateY: {PlayerControllerRotateY}");
        }

        yawDelta = Mathf.Clamp(yawDelta, -MaxAngularVelocityY, MaxAngularVelocityY);
        targetRotation = Quaternion.AngleAxis(yawDelta, Vector3.up) * targetRotation;

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

    private bool IsGrounded() {
        if (PlayerCollider == null)
        {
            return false;
        }

        Bounds bounds = PlayerCollider.bounds;
        Vector3 origin = bounds.center + Vector3.up * 0.01f;
        float groundCheckDistance = bounds.extents.y + 0.05f;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance, ~0, QueryTriggerInteraction.Ignore);
    }

    private float GetFlappingWingSpeed() {
        if (LeftHandDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out var leftVelocity) &&
            RightHandDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out var rightVelocity))
        {
            float leftFlap = Vector3.Dot(leftVelocity, Vector3.down);
            float rightFlap = Vector3.Dot(rightVelocity, Vector3.down);
            return (leftFlap + rightFlap) * 0.5f;
        }
        return 0.0f;
    }

    private void ResetPlayerPose(bool resetRotation)
    {
        if (HeadDevice.isValid &&
            HeadDevice.TryGetFeatureValue(CommonUsages.devicePosition, out var initialPosition) &&
            HeadDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out var initialRotation))
        {
            CenterPosition = initialPosition;
            ForwardRotation = initialRotation.eulerAngles.y;

            if (resetRotation)
            {
                transform.rotation = Quaternion.Euler(0.0f, ForwardRotation, 0.0f);
                FixRotationTarget.localRotation = Quaternion.identity;
            }
            else
            {
                FixRotationTarget.localRotation = Quaternion.Euler(0.0f, -ForwardRotation, 0.0f);
            }
        }
    }
}
