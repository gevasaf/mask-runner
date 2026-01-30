using UnityEngine;

/// <summary>
/// Settings for the ChocoCup power-up item.
/// Create an instance of this ScriptableObject to configure ChocoCup behavior.
/// </summary>
[CreateAssetMenu(fileName = "ChocoCupSettings", menuName = "Power-Ups/ChocoCup Settings", order = 1)]
public class ChocoCupSettings : PowerUpSettings
{
    [Header("ChocoCup Settings")]
    [Tooltip("Speed multiplier for ChocoCup (default: 2.0)")]
    public float speedMultiplier = 2f;
    
    [Tooltip("Duration of speed boost in seconds (default: 5.0)")]
    public float speedBoostDuration = 5f;
    
    void OnEnable()
    {
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = "ChocoCup";
        }
    }
}
