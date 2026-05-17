using UnityEngine;

public class Floating : MonoBehaviour
{
    [Header("浮動設定")]
    [Tooltip("上下浮動的最大距離 (中心點往上或往下的最大值)")]
    public float amplitude = 20f;

    [Tooltip("完成一次完整上下浮動循環所需的時間 (秒)")]
    public float period = 40f;

    // 紀錄物件初始位置
    private Vector3 startPos;
    
    // 儲存每個物件隨機生成的初始相位
    private float randomPhase;

    void Start()
    {
        // 紀錄初始位置。使用 localPosition 確保當物件有父物件時，相對位置也能正確運算
        startPos = transform.localPosition;

        // 生成一個 0 到 2π 的隨機相位值，確保每個物件的起伏時間點不同
        randomPhase = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        // 避免除以零的錯誤
        if (period <= 0f) return;

        // 根據 Sine 函數計算當前的 Y 軸位移
        // 公式: A * sin((2π / T) * t + φ)
        float theta = (Time.time * 2f * Mathf.PI / period) + randomPhase;
        float yOffset = amplitude * Mathf.Sin(theta);

        // 更新物件位置
        transform.localPosition = new Vector3(startPos.x, startPos.y + yOffset, startPos.z);
    }
}