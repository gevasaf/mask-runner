using UnityEngine;

/// <summary>
/// Settings for the MilkCup power-up item.
/// Create an instance of this ScriptableObject to configure MilkCup behavior.
/// </summary>
[CreateAssetMenu(fileName = "MilkCupSettings", menuName = "Power-Ups/MilkCup Settings", order = 2)]
public class MilkCupSettings : PowerUpSettings
{
    [Header("MilkCup Settings")]
    [Tooltip("Number of coins to add when collected (default: 10)")]
    public int coinsToAdd = 10;
    
    void OnEnable()
    {
        if (string.IsNullOrEmpty(displayName))
        {
            displayName = "MilkCup";
        }
    }
}
