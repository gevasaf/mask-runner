using UnityEngine;

/// <summary>
/// Base class for power-up settings. Each power-up type should have its own settings class.
/// </summary>
public abstract class PowerUpSettings : ScriptableObject
{
    [Header("General Settings")]
    [Tooltip("Display name for this power-up")]
    public string displayName = "Power-Up";
}
