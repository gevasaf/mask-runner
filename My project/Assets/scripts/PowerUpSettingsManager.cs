using UnityEngine;

/// <summary>
/// Manager component for power-up settings.
/// Attach this to a GameObject in your scene to manage all power-up settings in one place.
/// This is especially useful for ChocoCup settings that need to be configured separately.
/// </summary>
public class PowerUpSettingsManager : MonoBehaviour
{
    [Header("Power-Up Settings")]
    [Tooltip("Settings for MilkCup power-up")]
    public MilkCupSettings milkCupSettings;
    
    [Tooltip("Settings for ChocoCup power-up")]
    public ChocoCupSettings chocoCupSettings;
    
    [Tooltip("Settings for Bandage power-up")]
    public BandageSettings bandageSettings;
    
    private static PowerUpSettingsManager instance;
    
    public static PowerUpSettingsManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<PowerUpSettingsManager>();
            }
            return instance;
        }
    }
    
    void Awake()
    {
        // Ensure only one instance exists
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    
    /// <summary>
    /// Gets the settings for a specific power-up type
    /// </summary>
    public PowerUpSettings GetSettings(PowerUpItem.ItemType itemType)
    {
        switch (itemType)
        {
            case PowerUpItem.ItemType.MilkCup:
                return milkCupSettings;
            case PowerUpItem.ItemType.ChocoCup:
                return chocoCupSettings;
            case PowerUpItem.ItemType.Bandage:
                return bandageSettings;
            default:
                return null;
        }
    }
    
    /// <summary>
    /// Gets ChocoCup settings specifically
    /// </summary>
    public ChocoCupSettings GetChocoCupSettings()
    {
        return chocoCupSettings;
    }
    
    /// <summary>
    /// Gets MilkCup settings specifically
    /// </summary>
    public MilkCupSettings GetMilkCupSettings()
    {
        return milkCupSettings;
    }
    
    /// <summary>
    /// Gets Bandage settings specifically
    /// </summary>
    public BandageSettings GetBandageSettings()
    {
        return bandageSettings;
    }
}
