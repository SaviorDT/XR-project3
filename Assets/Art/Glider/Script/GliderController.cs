using UnityEngine;

/// <summary>
/// Glider 專用控制器 (純淨版)：支援後端 API 呼叫，並整合 Rigidbody 物理/IK 狀態切換。
/// </summary>
public class GliderController : MonoBehaviour
{
    // ==========================================
    // 模組 A：線性映射系統 (API -> Control -> Elevator)
    // ==========================================
    [System.Serializable]
    public class ControlMapping
    {
        public Transform controlStick;
        public Vector2 stickYLimit;
        
        public Transform elevator;
        public Vector3 elevatorRotMin;
        public Vector3 elevatorRotMax;

        private float currentVal = 0.5f;

        // API 呼叫接口
        public void SetValue(float normalizedVal)
        {
            currentVal = Mathf.Clamp01(normalizedVal);
        }

        public void UpdateMapping()
        {
            if (!controlStick || !elevator) return;
            
            // 1. 更新把手視覺位置
            Vector3 stickPos = controlStick.localPosition;
            stickPos.y = Mathf.Lerp(stickYLimit.x, stickYLimit.y, currentVal);
            controlStick.localPosition = stickPos;

            // 2. 更新 Elevator 旋轉
            elevator.localEulerAngles = new Vector3(
                Mathf.LerpAngle(elevatorRotMin.x, elevatorRotMax.x, currentVal),
                Mathf.LerpAngle(elevatorRotMin.y, elevatorRotMax.y, currentVal),
                Mathf.LerpAngle(elevatorRotMin.z, elevatorRotMax.z, currentVal)
            );
        }
    }

    // ==========================================
    // 模組 B：剛體 IK 系統 (API -> Wing & RB Handle)
    // ==========================================
    [System.Serializable]
    public class HandleIKMapping
    {
        [Header("骨架設定 (Hierarchy)")]
        public Transform wingPivot;
        public Transform handleRoot;
        public Transform handleTip;

        [Header("物理設定 (傳統 Rigidbody)")]
        [Tooltip("掛在擺盪部位 (Tip) 上的 Rigidbody")]
        public Rigidbody handleTipRB;
        public float inputTimeout = 0.05f; 

        [Header("IK 參數")]
        public Vector3 hingeAxis = Vector3.right;
        public bool invertBend;

        private float R; 
        private float L; 
        private bool isInit;

        // --- 狀態機變數 ---
        private bool isPhysicsDriven = true; 
        private Vector3 activeTargetLocal; // 儲存 Local Space 座標，跟隨滑翔傘移動
        private float lastInputTime = -999f;

        private CharacterJoint handleJoint;
        private Rigidbody cachedConnectedBody;

        public void Init(Transform root)
        {
            if (wingPivot && handleRoot && handleTip)
            {
                R = Vector3.Distance(wingPivot.position, handleRoot.position);
                L = Vector3.Distance(handleRoot.position, handleTip.position);
                isInit = true;

                activeTargetLocal = root.InverseTransformPoint(handleTip.position);

                handleJoint = handleRoot.GetComponent<CharacterJoint>();

                if (handleJoint)
                {
                    cachedConnectedBody = handleJoint.connectedBody;
                    handleJoint.autoConfigureConnectedAnchor = false;
                }
                
                SetPhysicsState(true, root); 
            }
        }

        public void SetTarget(Vector3 targetWorldPos, Transform root)
        {
            SetPhysicsState(false, root); 
            lastInputTime = Time.time;

            Vector3 targetLocal = root.InverseTransformPoint(targetWorldPos);

            if (Vector3.Distance(activeTargetLocal, targetLocal) > 0.001f)
            {
                activeTargetLocal = targetLocal;
            }
        }

        private void SetPhysicsState(bool enablePhysics, Transform root)
        {
            if (handleTipRB == null || isPhysicsDriven == enablePhysics) return;
            isPhysicsDriven = enablePhysics;

            if (isPhysicsDriven)
            {
                // 放手：關閉 Kinematic，讓重力接管
                if (handleJoint)
                {
                    handleJoint.connectedBody = cachedConnectedBody;
                    handleJoint.connectedAnchor = root.InverseTransformPoint(handleRoot.position);
                }
                handleTipRB.isKinematic = false; 
                handleTipRB.WakeUp(); 
            }
            else
            {
                // 抓緊：開啟 Kinematic，變成完全受腳本支配的硬物
                if (handleJoint)
                {
                    handleJoint.connectedBody = null; 
                }
                handleTipRB.isKinematic = true;     
            }
        }

        public void UpdateIK(Transform root)
        {
            if (!isInit) return;

            bool isUserLetGo = (Time.time - lastInputTime) > inputTimeout;

            if (isPhysicsDriven)
            {
                activeTargetLocal = root.InverseTransformPoint(handleTip.position);

                float slerpSpeed = 6f; 
                wingPivot.localRotation = Quaternion.Slerp(
                    wingPivot.localRotation, 
                    Quaternion.identity, 
                    slerpSpeed * Time.deltaTime
                );

                return;
            }

            // --- 執行 IK 解算 ---
            Vector3 activeTargetWorldPos = root.TransformPoint(activeTargetLocal);
            Vector3 pivotPos = wingPivot.position;
            Vector3 toTarget = activeTargetWorldPos - pivotPos;
            
            if (toTarget.sqrMagnitude < 0.0001f) return;

            Vector3 worldHinge = wingPivot.TransformDirection(hingeAxis).normalized;
            Vector3 currentAnchorPos = handleRoot.position;
            Vector3 currentAnchorDir = (currentAnchorPos - pivotPos).normalized;

            float d = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(R - L), R + L);
            float cosTheta = (d * d + R * R - L * L) / (2f * d * R);
            float theta = Mathf.Acos(Mathf.Clamp(cosTheta, -1f, 1f)) * Mathf.Rad2Deg;

            Vector3 projectedTarget = Vector3.ProjectOnPlane(toTarget, worldHinge).normalized;
            float sign = invertBend ? 1f : -1f;
            Vector3 solvedDir = Quaternion.AngleAxis(theta * sign, worldHinge) * projectedTarget;

            Vector3 currentProj = Vector3.ProjectOnPlane(currentAnchorDir, worldHinge).normalized;
            float angleToRotate = Vector3.SignedAngle(currentProj, solvedDir, worldHinge);

            wingPivot.rotation = Quaternion.AngleAxis(angleToRotate, worldHinge) * wingPivot.rotation;

            Vector3 ropeDir = (activeTargetWorldPos - handleRoot.position).normalized;
            handleRoot.up = -ropeDir;
            handleTip.position = handleRoot.position + ropeDir * L;

            if (isUserLetGo)
            {
                SetPhysicsState(true, root); 
            }
        }
    }

    // ==========================================
    // 實體宣告與 API 對外接口
    // ==========================================
    [Header("Controls -> Elevators (API Mapping)")]
    public ControlMapping leftControl;
    public ControlMapping rightControl;

    [Header("IK Targets -> Wings & Handles (API & Physics)")]
    public HandleIKMapping leftWingIK;
    public HandleIKMapping rightWingIK;

    private void Start()
    {
        // 傳入滑翔傘本體的 transform 作為座標轉換基準
        leftWingIK.Init(transform);
        rightWingIK.Init(transform);
    }

    private void LateUpdate()
    {
        leftControl.UpdateMapping();
        rightControl.UpdateMapping();
        
        leftWingIK.UpdateIK(transform);
        rightWingIK.UpdateIK(transform);
    }

    // --- 4 個提供給後端的 Public API ---

    public void SetLeftControlVal(float normalizedVal)
    {
        leftControl.SetValue(normalizedVal);
    }

    public void SetRightControlVal(float normalizedVal)
    {
        rightControl.SetValue(normalizedVal);
    }

    public void SetLeftHandleTargetLocation(Vector3 targetLocation)
    {
        leftWingIK.SetTarget(targetLocation, transform);
    }

    public void SetRightHandleTargetLocation(Vector3 targetLocation)
    {
        rightWingIK.SetTarget(targetLocation, transform);
    }
}