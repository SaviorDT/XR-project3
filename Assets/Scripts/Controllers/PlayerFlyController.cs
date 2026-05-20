using System;
using UnityEngine;
using UnityEngine.XR;

public class PlayerFlyController : MonoBehaviour
{
    [SerializeField] private float VelocitySteeringRatio = 1.0f;
    [SerializeField] private float PlayerRollThreshold = 5.0f;
    [SerializeField] private float RollSteeringRatio = 1.0f;
    [Tooltip("玩家的平移距離超過此閾值才會被視為俯衝或抬頭")]
    [SerializeField] private float PlayerPitchThreshold = 0.1f;
    [SerializeField] private float Gravity = 30f;
    [SerializeField] private float WindForce = 1.0f;
    [SerializeField] private float DownToForwardRatio = 4.0f, DownToForwardLossRatio = 0.5f;
    [SerializeField] private float VelocityToUpRatio = 4.0f, VelocityToUpLossRatio = 0.3f;
    [Tooltip("1秒後，玩家的速度會有多少比例轉向當前的飛行方向")]
    [SerializeField] private float SteeringSpeed = 2.0f;
    [SerializeField] private Vector3 WindResistance = new(0.5f, 0.1f, 0.5f);
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
    [SerializeField] private float DiveAngle = -82.0f, ClimbAngle = 82.0f;
    private Vector3 WindVelocity = Vector3.zero;
    private InputDevice HeadDevice;
    
    void Start()
    {
    }

    void Update()
    {
        if (!HeadDevice.isValid)
        {
            HeadDevice = InputDevices.GetDeviceAtXRNode(XRNode.Head);
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
            if (Mathf.Abs(headPosition.z) > PlayerPitchThreshold)
            {
                PlayerPitchState = headPosition.z > 0.0f ? EPitchState.Down : EPitchState.Up;
            }
            else
            {
                PlayerPitchState = EPitchState.Neutral;
            }
        }
    }
    
    void FixedUpdate()
    {
        // 轉向
        Vector3 horizontalVelocity = new(Velocity.x, 0.0f, Velocity.z);
        if (horizontalVelocity.sqrMagnitude > 0.0001f)
        {
            Vector3 horizontalForward = new Vector3(transform.forward.x, 0.0f, transform.forward.z).normalized;
            float theta = Vector3.SignedAngle(horizontalForward, horizontalVelocity.normalized, Vector3.up);
            Vector3 euler = transform.eulerAngles;
            euler.y += theta * VelocitySteeringRatio * Time.fixedDeltaTime;
            if (Mathf.Abs(PlayerRoll) > PlayerRollThreshold)
            {
                euler.y -= PlayerRoll * RollSteeringRatio * Time.fixedDeltaTime;
            }
            transform.rotation = Quaternion.Euler(euler);
        }

        // 計算俯仰角
        float pitch = transform.eulerAngles.x;
        if (PlayerPitchState == EPitchState.Up)
        {
            pitch += ClimbAngle;
        }
        else if (PlayerPitchState == EPitchState.Down)
        {
            pitch += DiveAngle;
        }
        pitch = Mathf.Clamp(pitch, -89.9f, 89.9f);

        // 重力加速度
        Velocity += Gravity * Time.fixedDeltaTime * Vector3.down;

        // 風力加速度
        if (WindVelocity.sqrMagnitude > 0.0001f)
        {
            Velocity += WindForce * Time.fixedDeltaTime * (WindVelocity - Vector3.Project(Velocity, WindVelocity.normalized));
        }

        // 下降轉往前
        if (Velocity.y < 0.0f)
        {
            float ReducedDownSpeed = -Velocity.y * DownToForwardRatio * Mathf.Cos(pitch * Mathf.Deg2Rad) * Mathf.Cos(pitch * Mathf.Deg2Rad) * Time.fixedDeltaTime;
            Velocity.y += ReducedDownSpeed;
            Velocity += ReducedDownSpeed * (1 - DownToForwardLossRatio) * transform.forward;
            Debug.Log($"Down to forward: {ReducedDownSpeed}, Velocity: {Velocity}, Pitch: {pitch}");
        }

        // 相對速度轉往上
        Vector3 RelativeVelocity = Velocity - WindVelocity;
        float ReducedForwardSpeed = -VelocityToUpRatio * 
                                    new Vector3(RelativeVelocity.x, 0, RelativeVelocity.z).magnitude * 
                                    Mathf.Sin(pitch * Mathf.Deg2Rad) * Mathf.Sin(pitch * Mathf.Deg2Rad) *
                                    Time.fixedDeltaTime;
        Velocity += ReducedForwardSpeed * RelativeVelocity.normalized;
        Velocity.y += ReducedForwardSpeed * (1 - VelocityToUpLossRatio) * (pitch > 0 ? -1 : 1);
        Debug.Log($"Forward to up: {ReducedForwardSpeed}, Velocity: {Velocity}, Pitch: {pitch}");

        // 速度轉向前面
        Vector3 ReducedSidewaysVelocity = SteeringSpeed * Time.fixedDeltaTime * -new Vector3(Velocity.x, 0.0f, Velocity.z);
        Velocity += ReducedSidewaysVelocity;
        Velocity -= Vector3.Project(ReducedSidewaysVelocity, transform.forward);

        // 風阻
        Velocity = Vector3.Scale(Velocity, WindResistance * Time.fixedDeltaTime + Vector3.one * (1 - Time.fixedDeltaTime));

        // 移動
        transform.position += Velocity * Time.fixedDeltaTime;
    }

    public void SetWindVelocity(Vector3 velocity)
    {
        WindVelocity += velocity;
    }
}
