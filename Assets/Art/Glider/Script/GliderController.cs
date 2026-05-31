using UnityEngine;

/// <summary>
/// Glider 專用控制器：支援後端 API 呼叫，並整合 ArticulationBody 物理/IK 狀態切換。
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

        private float currentVal = 0.5f; // 預設 0.5 (置中)

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
    // 模組 B：剛體 IK 系統 (改用 Rigidbody 系統)
    // ==========================================
    [System.Serializable]
    public class HandleIKMapping
    {
        [Header("骨架設定 (Hierarchy)")]
        public Transform wingPivot;
        public Transform ropeAnchor;
        public Transform handleRoot;
        public Transform handleTip;

        [Header("物理設定 (傳統 Rigidbody)")]
        [Tooltip("掛在 Handle Tip 上的 Rigidbody")]
        public Rigidbody handleTipRB;
        public float reachThreshold = 0.01f;
        public float inputTimeout = 0.05f; 

        [Header("IK 參數")]
        public Vector3 hingeAxis = Vector3.right;
        public bool invertBend;

        private float R; 
        private float L; 
        private bool isInit;

        // --- 狀態機變數 ---
        private bool isPhysicsDriven = true; 
        [HideInInspector] public Vector3 activeTargetPos;
        private float lastInputTime = -999f;

        public void Init()
        {
            if (wingPivot && ropeAnchor && handleRoot && handleTip)
            {
                R = Vector3.Distance(wingPivot.position, ropeAnchor.position);
                L = Vector3.Distance(ropeAnchor.position, handleTip.position);
                isInit = true;

                activeTargetPos = handleTip.position;
                
                SetPhysicsState(true); 
            }
        }

        public void SetTarget(Vector3 targetPos)
        {
            lastInputTime = Time.time;

            if (Vector3.Distance(activeTargetPos, targetPos) > 0.001f)
            {
                activeTargetPos = targetPos;
            }
            
            SetPhysicsState(false); 
        }

        // 核心改變：切換 Rigidbody 的 Kinematic 狀態
        private void SetPhysicsState(bool enablePhysics)
        {
            if (handleTipRB == null || isPhysicsDriven == enablePhysics) return;
            isPhysicsDriven = enablePhysics;

            if (isPhysicsDriven)
            {
                // 放手：關閉 Kinematic，讓重力與 Character Joint 接管
                handleTipRB.isKinematic = false; 
                
                // 消除 IK 殘留的假動能，確保它自然落下
                handleTipRB.linearVelocity = Vector3.zero;
                handleTipRB.angularVelocity = Vector3.zero;
                handleTipRB.WakeUp(); 
            }
            else
            {
                // 抓緊：開啟 Kinematic，變成完全受腳本支配的硬物
                handleTipRB.isKinematic = true;     
            }
        }

        public void UpdateIK()
        {
            if (!isInit) return;

            bool isUserLetGo = (Time.time - lastInputTime) > inputTimeout;

            if (isPhysicsDriven)
            {
                // 物理狀態下，我們只需要負責把錨點 (Root) 放在繩子綁點上即可
                // Tip 會因為 Character Joint 的關係自己甩動
                handleRoot.position = ropeAnchor.position;
                return;
            }

            // --- 執行 IK 解算 ---
            Vector3 pivotPos = wingPivot.position;
            Vector3 toTarget = activeTargetPos - pivotPos;
            
            if (toTarget.sqrMagnitude < 0.0001f) return;

            Vector3 worldHinge = wingPivot.TransformDirection(hingeAxis).normalized;
            Vector3 currentAnchorPos = ropeAnchor.position;
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

            Vector3 newAnchorPos = pivotPos + (Quaternion.AngleAxis(angleToRotate, worldHinge) * (currentAnchorPos - pivotPos));
            Vector3 ropeDir = (activeTargetPos - newAnchorPos).normalized;

            handleRoot.position = newAnchorPos;
            handleRoot.up = -ropeDir;
            
            // 因為是 Kinematic，Tip 不會受物理影響，我們必須強迫它的 Transform 對齊 Root
            // 確保切換回物理時，它是在正確的位置上
            handleTip.position = handleRoot.position + ropeDir * L;

            // --- Reach 判定與狀態切換 ---
            Vector3 solvedTipPos = pivotPos + projectedTarget * d;
            bool isReached = Vector3.Distance(handleTip.position, solvedTipPos) <= reachThreshold;

            if (isReached && isUserLetGo)
            {
                SetPhysicsState(true); 
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


    private void LateUpdate()
    {
        leftControl.UpdateMapping();
        rightControl.UpdateMapping();
        
        leftWingIK.UpdateIK();
        rightWingIK.UpdateIK();

        bool isManualDragging = enableTestModule && (testMode == TestMode.ManualDrag);
        
        if (!isManualDragging)
        {
            if (testLeftTarget) testLeftTarget.position = leftWingIK.activeTargetPos;
            if (testRightTarget) testRightTarget.position = rightWingIK.activeTargetPos;
        }
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
        leftWingIK.SetTarget(targetLocation);
    }

    public void SetRightHandleTargetLocation(Vector3 targetLocation)
    {
        rightWingIK.SetTarget(targetLocation);
    }

// ==========================================
    // 測試專用模組：手動拖曳 / 自動畫圓 / 物理晃動
    // ==========================================
    public enum TestMode
    {
        ManualDrag, // 模式 1：允許你在 Scene 中拖曳 Target 物件來測試 IK
        AutoCircle, // 模式 2：自動畫圓 (模擬後端連續發送變動座標)
        LetGo       // 模式 3：停止發送 API，測試物理擺盪
    }

    [Header("自動化測試 (Auto Test)")]
    public bool enableTestModule = false;
    
    [Tooltip("切換測試模式")]
    public TestMode testMode = TestMode.ManualDrag;

    [Header("手動測試目標 (對應 ManualDrag 模式)")]
    [Tooltip("將你原本的 Left Pull Target 拖進來")]
    public Transform testLeftTarget;
    [Tooltip("將你原本的 Right Pull Target 拖進來")]
    public Transform testRightTarget;

    private Quaternion originalRotation;

    private Vector3 leftBase;
    private Vector3 rightBase;

    private void Start()
    {
        leftWingIK.Init();
        rightWingIK.Init();
        originalRotation = transform.rotation;
        leftBase = leftWingIK.ropeAnchor.position - transform.up * 0.4f;
        rightBase = rightWingIK.ropeAnchor.position - transform.up * 0.4f;
    }

    private void Update()
    {
        if (!enableTestModule) return;

        if (testMode == TestMode.ManualDrag)
        {
            // --- 狀態 1：手動拖曳模式 ---
            // 讀取你拖進來的空物件座標，並丟給 API。這樣你就可以在畫面上自由拖拉測試了！
            if (testLeftTarget) SetLeftHandleTargetLocation(testLeftTarget.position);
            if (testRightTarget) SetRightHandleTargetLocation(testRightTarget.position);
            
            // 為了不干擾，手把數值先維持置中
            SetLeftControlVal(0.5f);
            SetRightControlVal(0.5f);

            // 回正滑翔傘
            transform.rotation = Quaternion.Lerp(transform.rotation, originalRotation, Time.deltaTime * 5f);
        }
        else if (testMode == TestMode.AutoCircle)
        {
            // --- 狀態 2：自動畫圓 IK 模式 ---
            float controlVal = (Mathf.Sin(Time.time) + 1f) / 2f; 
            SetLeftControlVal(controlVal);
            SetRightControlVal(1f - controlVal); 

            float circleSpeed = 1.5f; 
            
            Vector3 leftOffset = (transform.forward * Mathf.Sin(Time.time * circleSpeed)) * 0.2f;
            Vector3 rightOffset = (transform.right * Mathf.Cos(Time.time * circleSpeed)) * 0.2f;

            SetLeftHandleTargetLocation(leftBase + leftOffset);
            SetRightHandleTargetLocation(rightBase + rightOffset);

            transform.rotation = Quaternion.Lerp(transform.rotation, originalRotation, Time.deltaTime * 5f);
        }
        else if (testMode == TestMode.LetGo)
        {
            // --- 狀態 3：放手物理擺盪模式 ---
            // 刻意不呼叫 SetTarget API，觸發 Timeout 釋放關節
            float rockAngle = Mathf.Sin(Time.time * 3f) * 20f;
            transform.rotation = originalRotation * Quaternion.Euler(0, 0, rockAngle);
        }
    }
}