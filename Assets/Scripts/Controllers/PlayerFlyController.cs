using System;
using UnityEngine;
using UnityEngine.XR;

[RequireComponent(typeof(Rigidbody))]
public class PlayerFlyController : MonoBehaviour
{
    [SerializeField] private bool DebugMode = false;
    [SerializeField] private bool InUniverse = false;
    [SerializeField] private bool FlyEnabled = true;
    [Header("Player Control Settings")]
    [SerializeField] private float PlayerHeight = 1.4f;
    [SerializeField] private float OnGroundRotateAngle = 30.0f;
    [SerializeField] private float OnGroundRotateCoolDown = 0.5f;
    [SerializeField] private Vector3 FlappingWingForce = new(0.0f, 0.6f, 0.6f);
    [SerializeField] private Vector3 TakeOffVelocity = new(0.0f, 10.0f, 3.0f);
    [SerializeField] private float FlappingWingThreshold = 0.5f;
    [SerializeField] private float MaxFlappingAmount = 30.0f;
    [SerializeField] private float FlyCoolDown = 20.0f;
    [SerializeField] private Vector3 FrontBarRPosition = new(0.0328f, 1.0518f, 0.194f);
    [SerializeField] private Vector3 FrontBarLPosition = new(-0.15f, 1.0518f, 0.194f);
    [SerializeField] private float FrontBarHeight = 0.16f, FrontBarLength = 0.15f;
    [SerializeField] private float FrontBarAttachDistance = 0.06f;
    [SerializeField] private float FrontBarResetSpeed = 3.0f;
    [SerializeField] private float FrontBarDistanceToPitchRatio = 80.0f;
    [SerializeField] private float FrontBarDistanceToRollRatio = -15.0f;
    [SerializeField] private float GliderPitchRatio = 0.5f, GliderPitchSpeed = 1.0f;
    [SerializeField] private float GliderRollRatio = 0.3f, GliderRollSpeed = 1.0f;
    [SerializeField] private Transform SideBarRPosition;
    [SerializeField] private Transform SideBarLPosition;
    [SerializeField] private float SideBarAttachDistance = 0.2f;
    [SerializeField] private float RollMinDiff = 0.1f;
    [Header("Flight Pose Settings")]
    [SerializeField] private float VelocitySteeringRatio = 0.5f;
    [SerializeField] private float CorrectPitchRatio = 0.5f;
    [SerializeField] private float MaxAngularVelocityY = 90.0f;
    [Header("Flight Physics Settings")]
    [SerializeField] private float Gravity = 9.8f;
    [SerializeField] private float ReducedGravityRatio = 0.75f;
    [SerializeField] private float StallSpeed = 5.0f;
    [SerializeField] private float WindForce = 1.0f;
    [SerializeField] private float DownToForwardRatio = 2.0f, DownToForwardLossRatio = 0.0f;
    [SerializeField] private float VelocityToUpRatio = 0.8f, VelocityToUpLossRatio = -2.2f;
    [Tooltip("1秒後，玩家的速度會有多少比例轉向當前的飛行方向")]
    [SerializeField] private float SteeringSpeed = 1.5f;
    [SerializeField] private Vector3 WindResistance = new(0.8f, 0.6f, 0.8f);
    [Header("References")]
    [SerializeField] private Transform FixPoseTarget;
    [SerializeField] private BoxCollider PlayerCollider;
    [SerializeField] private Transform CameraTransform;
    [SerializeField] private GliderController _GliderController;
    [SerializeField] private Transform FixGliderTarget;
    [SerializeField] private GameObject[] FlappableWingEffect;
    // 1. 偵測是否在地面，如果是，除了起飛以外不進行其他運算
    // 2. 偵測起飛、拍翅膀

    // 調整姿態
    // 3. 轉向目前速度方向
    // 4. 將玩家輸入套用到轉向
    // 5. 計算俯仰角

    // 計算速度
    // 6. 套用重力加速度
    // 7. 套用風力加速度
    // 8. 將當前往下的速度轉換為往前的速度
    // 9. 將當前往前的速度轉換為往上的速度
    // 10. 將速度轉向前面（避免轉向後持續橫向飛行）
    // 11. 套用風阻
    
    [Header("Sound Settings")]
    [SerializeField] private AudioSource RingReadyAudio;
    [SerializeField] private AudioSource WindAudio, ClothAudio, FlapAudio;
    [SerializeField] private float ClothVolumeMax = 0.8f, ClothVolumeMin = 0.3f;
    [SerializeField] private float PlayClothSpeedThreshold = 6.0f, ClothVolumeSpeedMax = 40.0f;
    private bool RingReadyPlayed = true, ClothPlayed = false, FlapPlayed = false;
    [SerializeField] private AudioSource TriggerButtonSound;
    private bool TriggerButtonPressedPrev = false;


    [Header("Debug variables(Don't modify)")]

    // 以下為計算用變數
    [SerializeField] private Vector3 Velocity = Vector3.zero;
    private float PlayerControllerYaw = 0.0f;
    private float GliderRoll = 0.0f;
    private float PlayerControllerPitch = 0.0f;
    private float GliderPitch = 0.0f;
    private bool GrabRPressed = false, GrabLPressed = false;
    private bool FrontBarRAttached = false, FrontBarLAttached = false;
    private bool SideBarRAttached = false, SideBarLAttached = false;
    [SerializeField] private Vector3 WindVelocity = Vector3.zero;
    private InputDevice HeadDevice;
    private InputDevice LeftHandDevice;
    private InputDevice RightHandDevice;
    private Rigidbody PlayerRigidbody;
    private Vector3 CenterPosition = Vector3.zero;
    private float ForwardRotation = 0.0f;
    private float NextOnGroundRotateTime = 0.0f;
    private float NextFlyTime = 0.0f;
    private float NextOngroundDetectTime = 0.0f;
    private bool IsFlapping = false;
    private float FlappingAmount = 0.0f;
    private float FlappingAmountBuffer = 0.0f;
    private float RightControllerLastY = 0.0f, LeftControllerLastY = 0.0f;
    private bool RightBPressedPrev = false;
    private int PauseFixedFrameCount = 0;
    
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

        // Detect Meta Quest 3 right controller B button (secondary button).
        if (RightHandDevice.isValid && RightHandDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out bool rightBPressed))
        {
            if (rightBPressed && !RightBPressedPrev)
            {
                InitPlayerPose();
            }
            RightBPressedPrev = rightBPressed;
        }

        if (CenterPosition == Vector3.zero)
        {
            InitPlayerPose();
        }

        if (IsGrounded())
        {
            NextFlyTime = 0.0f;
        }

        if (Velocity.magnitude > PlayClothSpeedThreshold && !InUniverse)
        {
            ClothAudio.volume = Mathf.Lerp(ClothVolumeMin, ClothVolumeMax, (Velocity.magnitude - PlayClothSpeedThreshold) / (ClothVolumeSpeedMax - PlayClothSpeedThreshold));
            if (!ClothPlayed)
            {
                ClothAudio.Play();
                ClothPlayed = true;
            }
        }
        else
        {
            if (ClothPlayed)
            {
                ClothAudio.Stop();
                ClothPlayed = false;
            }
        }

        TryAttachBar();
        TryDetectPlayerInput();
        TryDetectFlapping();
        TryRotateOnGround();
        TryResetFrontBar();
        PlayTriggerButtonSound();
        DebugInput();
    }
    
    void FixedUpdate()
    {
        if (PauseFixedFrameCount > 0)
        {
            PauseFixedFrameCount--;
            return;
        }
        PlayerRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        PlayerRigidbody.isKinematic = false;

        // 著地時不飛行
        if (IsGrounded())
        {
            // 起飛
            if (FlappingAmount > 0.001f)
            {
                // ResetPlayerPose();
                Velocity = transform.TransformDirection(TakeOffVelocity * FlappingAmount / MaxFlappingAmount);
                NextOngroundDetectTime = Time.time + 0.041f;
            }
            else {
                Velocity = Vector3.zero;
            }
            FlappingAmount = 0.0f;
            PlayerRigidbody.linearVelocity = Velocity;
            PlayerRigidbody.angularVelocity = Vector3.zero;
            return;
        }

        // 空中揮翅
        if (FlappingAmount > 0.001f)
        {
            Velocity += FlappingAmount * transform.TransformDirection(FlappingWingForce);
            FlappingAmount = 0.0f;
        }

        Vector3 oldVelocity = Velocity;

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

        // 玩家控制轉向
        yawDelta += PlayerControllerYaw;

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
        pitch += PlayerControllerPitch;

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
            Velocity += ReducedDownSpeed * (1 - DownToForwardLossRatio) * new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        }

        // 相對速度轉往上
        Vector3 RelativeVelocity = Velocity - WindVelocity;
        float ReducedForwardSpeed = -VelocityToUpRatio * 
                                    new Vector3(RelativeVelocity.x, 0, RelativeVelocity.z).magnitude * 
                                    Mathf.Abs(Mathf.Sin(pitch)) *
                                    Time.fixedDeltaTime;
        
        if (pitch > 0 || InUniverse)
        {
            Velocity += ReducedForwardSpeed * RelativeVelocity.normalized;
            Velocity.y += ReducedForwardSpeed * (1 - VelocityToUpLossRatio) * (pitch > 0 ? -1 : 1);
        }

        // 速度轉向前面
        Vector3 ReducedSidewaysVelocity = SteeringSpeed * Time.fixedDeltaTime * -new Vector3(Velocity.x, 0.0f, Velocity.z);
        Velocity += ReducedSidewaysVelocity;
        Velocity -= Vector3.Project(ReducedSidewaysVelocity, transform.forward);


        // 風阻
        Velocity.x = Mathf.Lerp(oldVelocity.x, Velocity.x, 0.9f);
        Velocity.z = Mathf.Lerp(oldVelocity.z, Velocity.z, 0.9f);
        if (new Vector3(Velocity.x, 0.0f, Velocity.z).magnitude > StallSpeed)
        {
            Velocity = Vector3.Scale(Velocity, WindResistance * Time.fixedDeltaTime + Vector3.one * (1 - Time.fixedDeltaTime));
        }

        // 移動
        PlayerRigidbody.linearVelocity = Velocity;
    }

    public void SetWindVelocity(Vector3 velocity)
    {
        WindVelocity += velocity;
        if (velocity.sqrMagnitude > 0.0001f)
        {
            WindAudio.Play();
        }
         else
        {
            WindAudio.Stop();
        }
    }

    public bool IsGrounded() {
        if (PlayerCollider == null)
        {
            return false;
        }
        if (Time.time < NextOngroundDetectTime)
        {
            return false;
        }

        Bounds bounds = PlayerCollider.bounds;
        Vector3 origin = bounds.center + Vector3.up * 0.01f;
        float groundCheckDistance = bounds.extents.y + 0.05f;
        int layerMask = ~(1 << PlayerCollider.gameObject.layer);
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance, layerMask, QueryTriggerInteraction.Ignore);
    }

    private void TryDetectFlapping() {
        if (Time.time < NextFlyTime || !FlyEnabled)
        {
            IsFlapping = false;
            FlapPlayed = false;
            RingReadyPlayed = false;
            FlappingAmount = FlappingAmountBuffer;
            FlappingAmountBuffer = 0.0f;
            foreach (var effect in FlappableWingEffect)
            {
                effect.SetActive(false);
            }
            return;
        }

        if (!RingReadyPlayed)
        {
            RingReadyAudio.Play();
            RingReadyPlayed = true;
        }
        foreach (var effect in FlappableWingEffect)
        {
            effect.SetActive(true);
        }

        if (!LeftHandDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out var leftVelocity) ||
            !RightHandDevice.TryGetFeatureValue(CommonUsages.deviceVelocity, out var rightVelocity) ||
            !LeftHandDevice.TryGetFeatureValue(CommonUsages.devicePosition, out var leftPosition) ||
            !RightHandDevice.TryGetFeatureValue(CommonUsages.devicePosition, out var rightPosition))
        {
            IsFlapping = false;
            FlapPlayed = false;
            FlappingAmount = FlappingAmountBuffer;
            FlappingAmountBuffer = 0.0f;
            return;
        }

        Vector3 leftWorldPosition = FixGliderTarget.TransformPoint(Quaternion.Inverse(Quaternion.Euler(0.0f, ForwardRotation, 0.0f)) * (leftPosition - CenterPosition + new Vector3(0, PlayerHeight, 0)));
        Vector3 rightWorldPosition = FixGliderTarget.TransformPoint(Quaternion.Inverse(Quaternion.Euler(0.0f, ForwardRotation, 0.0f)) * (rightPosition - CenterPosition + new Vector3(0, PlayerHeight, 0)));

        if (SideBarLAttached)
        {
            _GliderController.SetLeftHandleTargetLocation(leftWorldPosition);
        }
        if (SideBarRAttached)
        {
            _GliderController.SetRightHandleTargetLocation(rightWorldPosition);
        }

        if (!SideBarLAttached || !SideBarRAttached)
        {
            IsFlapping = false;
            FlapPlayed = false;
            FlappingAmount = FlappingAmountBuffer;
            FlappingAmountBuffer = 0.0f;
            return;
        }

        float leftFlap = Vector3.Dot(leftVelocity, Vector3.down);
        float rightFlap = Vector3.Dot(rightVelocity, Vector3.down);
        float currentFlappingAmount = leftFlap + rightFlap;

        if (currentFlappingAmount > FlappingWingThreshold)
        {
            if (!IsFlapping)
            {
                IsFlapping = true;
                FlappingAmountBuffer = 0.0f;
            }
            if (!FlapPlayed)
            {
                FlapAudio.Play();
                FlapPlayed = true;
            }

            FlappingAmountBuffer += currentFlappingAmount;
            return;
        }

        if (!IsFlapping)
        {
            return;
        }

        IsFlapping = false;
        FlapPlayed = false;
        Debug.Log($"Flapping Amount: {FlappingAmountBuffer}");
        FlappingAmount = Mathf.Clamp(FlappingAmountBuffer, 0.0f, MaxFlappingAmount);
        FlappingAmountBuffer = 0.0f;
        NextFlyTime = Time.time + FlyCoolDown;
    }

    private void InitPlayerPose()
    {
        if (HeadDevice.isValid &&
            HeadDevice.TryGetFeatureValue(CommonUsages.devicePosition, out var initialPosition) &&
            HeadDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out var initialRotation))
        {
            CenterPosition = initialPosition;
            ForwardRotation = initialRotation.eulerAngles.y;

            FixPoseTarget.SetLocalPositionAndRotation(new Vector3(0, PlayerHeight, 0) - Quaternion.Euler(0.0f, -ForwardRotation, 0.0f) * new Vector3(CenterPosition.x, CenterPosition.y, CenterPosition.z), 
                                                            Quaternion.Euler(0.0f, -ForwardRotation, 0.0f));
        }
    }


    void TryAttachBar()
    {
        TryAttachBarForHand(
            RightHandDevice,
            FrontBarRPosition,
            FrontBarLength,
            SideBarRPosition,
            FixGliderTarget,
            ref GrabRPressed,
            ref FrontBarRAttached,
            ref SideBarRAttached
        );

        TryAttachBarForHand(
            LeftHandDevice,
            FrontBarLPosition,
            FrontBarLength,
            SideBarLPosition,
            FixGliderTarget,
            ref GrabLPressed,
            ref FrontBarLAttached,
            ref SideBarLAttached
        );
    }

    private void TryAttachBarForHand(
        InputDevice handDevice,
        Vector3 frontBarLocalPosition,
        float frontBarLength,
        Transform sideBarTransform,
        Transform rootTransform,
        ref bool grabPressed,
        ref bool frontBarAttached,
        ref bool sideBarAttached)
    {
        if (!handDevice.isValid || !handDevice.TryGetFeatureValue(CommonUsages.gripButton, out bool isGrabPressed) || !isGrabPressed)
        {
            grabPressed = false;
            frontBarAttached = false;
            sideBarAttached = false;
            return;
        }

        if (grabPressed)
        {
            return;
        }

        grabPressed = true;
        frontBarAttached = false;
        sideBarAttached = false;

        if (handDevice.TryGetFeatureValue(CommonUsages.devicePosition, out var controllerPosition))
        {
            Vector3 relativeControllerPosition = Quaternion.Inverse(Quaternion.Euler(0.0f, ForwardRotation, 0.0f)) * (controllerPosition - CenterPosition + new Vector3(0, PlayerHeight, 0));
            
            // Treat the front bar as a line segment from frontBarLocalPosition
            // to frontBarLocalPosition + (frontBarLength, 0, 0) and check
            // the shortest distance from the controller to that segment.
            Vector3 a = frontBarLocalPosition;
            Vector3 b = frontBarLocalPosition + new Vector3(frontBarLength, 0.0f, 0.0f);
            Vector3 ap = relativeControllerPosition - a;
            Vector3 ab = b - a;
            float abSqr = Vector3.Dot(ab, ab);
            float t = 0.0f;
            if (abSqr > 1e-6f)
            {
                t = Mathf.Clamp(Vector3.Dot(ap, ab) / abSqr, 0.0f, 1.0f);
            }
            Vector3 closest = a + ab * t;
            if (Vector3.Distance(relativeControllerPosition, closest) <= FrontBarAttachDistance)
            {
                frontBarAttached = true;
                // frontBarAttachY = controllerPosition.y;
            }

            if (sideBarTransform != null)
            {
                Vector3 relativeSideBarPos = rootTransform.InverseTransformPoint(sideBarTransform.position);
                if (Vector3.Distance(relativeControllerPosition, relativeSideBarPos) <= SideBarAttachDistance)
                {
                    sideBarAttached = true;
                }
            }
        }
    }

    private void TryDetectPlayerInput()
    {
        PlayerControllerYaw = 0.0f;
        PlayerControllerPitch = 0.0f;

        if (!RightHandDevice.TryGetFeatureValue(CommonUsages.devicePosition, out var rightControllerPosition) ||
            !LeftHandDevice.TryGetFeatureValue(CommonUsages.devicePosition, out var leftControllerPosition))
        {
            return;
        }

        float rightControllerY = rightControllerPosition.y - CenterPosition.y - FrontBarRPosition.y + PlayerHeight;
        rightControllerY = rightControllerY / FrontBarHeight + 0.5f;
        rightControllerY = Mathf.Clamp(rightControllerY, 0.0f, 1.0f);
        float leftControllerY = leftControllerPosition.y - CenterPosition.y - FrontBarLPosition.y + PlayerHeight;
        leftControllerY = leftControllerY / FrontBarHeight + 0.5f;
        leftControllerY = Mathf.Clamp(leftControllerY, 0.0f, 1.0f);

        if (FrontBarRAttached)
        {
            _GliderController.SetRightControlVal(rightControllerY);
            RightControllerLastY = rightControllerY;
        }
        if (FrontBarLAttached)
        {
            _GliderController.SetLeftControlVal(leftControllerY);
            LeftControllerLastY = leftControllerY;
        }

        if (!FrontBarRAttached || !FrontBarLAttached)
        {
            GliderRoll = Mathf.Lerp(GliderRoll, 0.0f, GliderRollSpeed * Time.deltaTime);
            GliderPitch = Mathf.Lerp(GliderPitch, 0.0f, GliderPitchSpeed * Time.deltaTime);

            FixGliderTarget.localRotation = Quaternion.Euler(GliderPitch, 0.0f, GliderRoll);

            return;
        }

        float controllerHeightDiff = rightControllerY - leftControllerY;
        if (Mathf.Abs(controllerHeightDiff) > RollMinDiff)
        {
            PlayerControllerYaw = controllerHeightDiff * FrontBarDistanceToRollRatio;
        }

        PlayerControllerPitch = (rightControllerY + leftControllerY - 1.0f) * FrontBarDistanceToPitchRatio;

        GliderRoll = Mathf.Lerp(GliderRoll, PlayerControllerYaw * GliderRollRatio, GliderRollSpeed * Time.deltaTime);
        GliderPitch = Mathf.Lerp(GliderPitch, PlayerControllerPitch * GliderPitchRatio, GliderPitchSpeed * Time.deltaTime);

        FixGliderTarget.localRotation = Quaternion.Euler(GliderPitch, 0.0f, GliderRoll);
    }

    private void TryRotateOnGround()
    {
        if (!IsGrounded())
        {
            return;
        }

        if (Time.time < NextOnGroundRotateTime)
        {
            return;
        }

        if (!LeftHandDevice.isValid ||
            !LeftHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftStick))
        {
            return;
        }

        float rotateDirection = 0.0f;
        if (leftStick.x <= -0.5f)
        {
            rotateDirection = -1.0f;
        }
        else if (leftStick.x >= 0.5f)
        {
            rotateDirection = 1.0f;
        }

        if (rotateDirection == 0.0f)
        {
            return;
        }

        float targetYaw = PlayerRigidbody.rotation.eulerAngles.y + rotateDirection * OnGroundRotateAngle;
        PlayerRigidbody.MoveRotation(Quaternion.Euler(0.0f, targetYaw, 0.0f));
        NextOnGroundRotateTime = Time.time + OnGroundRotateCoolDown;
    }

    private void TryResetFrontBar()
    {
        if (!FrontBarRAttached)
        {
            float RightControllerNewY = Mathf.Lerp(RightControllerLastY, 0.5f, FrontBarResetSpeed * Time.deltaTime);
            RightControllerLastY = RightControllerNewY;
            _GliderController.SetRightControlVal(RightControllerNewY);
        }
        if (!FrontBarLAttached)
        {
            float LeftControllerNewY = Mathf.Lerp(LeftControllerLastY, 0.5f, FrontBarResetSpeed * Time.deltaTime);
            LeftControllerLastY = LeftControllerNewY;
            _GliderController.SetLeftControlVal(LeftControllerNewY);
        }
    }

    private void DebugInput()
    {
        if (!DebugMode)
        {
            return;
        }

        if (!HeadDevice.isValid)
        {
            HeadDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        }

        if (HeadDevice.isValid && HeadDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 devicePos))
        {
            devicePos -= CenterPosition;
            PlayerControllerYaw = Mathf.Clamp(devicePos.x * 90.0f, -15.0f, 15.0f);
            PlayerControllerPitch = Mathf.Clamp(devicePos.z * 90.0f, -80.0f, 80.0f);
        }

        if (!RightHandDevice.isValid)
        {
            RightHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        }

        if (RightHandDevice.isValid && RightHandDevice.TryGetFeatureValue(CommonUsages.primaryButton, out bool aPressed) && aPressed)
        {
            FlappingAmount = 10.0f;
            NextFlyTime = Time.time + FlyCoolDown;
        }
    }

    private void PlayTriggerButtonSound()
    {
        bool triggerPressed = false;
        if (RightHandDevice.isValid)
        {
            RightHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);
        }
        if (!triggerPressed && LeftHandDevice.isValid)
        {
            LeftHandDevice.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed);
        }

        if (!triggerPressed)
        {
            TriggerButtonPressedPrev = false;
            return;
        }

        if (triggerPressed && !TriggerButtonPressedPrev)
        {
            TriggerButtonSound.Play();
        }

        TriggerButtonPressedPrev = triggerPressed;
    }
    public void EnableFly()
    {
        FlyEnabled = true;
    }
    public void DisableFly()
    {
        FlyEnabled = false;
    }
    public void SetTransform(Vector3 position, Quaternion rotation, bool resetVelocity = false)
    {
        if (DebugMode)
        {
            return;
        }
        PlayerRigidbody.interpolation = RigidbodyInterpolation.None;
        PauseFixedFrameCount = 2;

        if (resetVelocity)
        {
            PlayerRigidbody.isKinematic = true;
            Velocity = Vector3.zero;
        }
        PlayerRigidbody.position = position;
        PlayerRigidbody.rotation = rotation;
    }
}
