using UnityEngine;

/// <summary>
/// Defines a world: display name and enemy prefabs. Floor/skybox opacities are derived from world index (1 at index, 0 elsewhere).
/// </summary>
[CreateAssetMenu(fileName = "WorldData", menuName = "Runner/World Data", order = 1)]
public class WorldData : ScriptableObject
{
    [Tooltip("Display name shown when entering this world (e.g. \"Desert\", \"Forest\")")]
    public string displayName = "World";

    [Header("Enemies")]
    [Tooltip("Enemy prefabs to spawn in this world.")]
    public GameObject[] enemyPrefabs = new GameObject[0];
}
