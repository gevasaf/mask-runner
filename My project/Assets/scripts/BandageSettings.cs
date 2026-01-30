using UnityEngine;

/// <summary>
/// Settings for the Bandage power-up item.
/// Create an instance of this ScriptableObject to configure Bandage behavior.
/// </summary>
[CreateAssetMenu(fileName = "BandageSettings", menuName = "Power-Ups/Bandage Settings", order = 3)]
public class BandageSettings : PowerUpSettings
{
    [Header("Bandage Settings")]
    [Tooltip("Number of lives to add when collected (default: 1)")]
    public int livesToAdd = 1;
    
    void OnEnable()
    {
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = "Bandage";
        }
    }
}
