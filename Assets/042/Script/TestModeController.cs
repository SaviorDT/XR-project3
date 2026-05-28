using UnityEngine;

public class TestModeController : MonoBehaviour
{
    public static TestModeController Instance { get; private set; }

    [Header("Test Mode")]
    public bool updateWindSettingsEveryFrame = true;

    private void Awake()
    {
        Instance = this;
    }

    public static bool ShouldUpdateEveryFrame()
    {
        if (Instance == null) return false;
        return Instance.updateWindSettingsEveryFrame;
    }
}