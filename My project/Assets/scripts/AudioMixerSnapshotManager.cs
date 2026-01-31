using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Manages audio mixer snapshots for different game worlds.
/// Transitions between snapshots when worlds change, using the same fade duration as visual transitions.
/// </summary>
public class AudioMixerSnapshotManager : MonoBehaviour
{
    [System.Serializable]
    public class WorldSnapshot
    {
        [Tooltip("Index of the world in GameManager's worlds array. Use the dropdown to select.")]
        [HideInInspector]
        public int worldIndex = 0;
        
        [Tooltip("Audio mixer snapshot to use for this world")]
        public AudioMixerSnapshot snapshot;
    }
    
    [Header("Initial Snapshot")]
    [Tooltip("Snapshot to use at game start (before any world transitions)")]
    public AudioMixerSnapshot initialSnapshot;
    
    [Header("World Snapshots")]
    [Tooltip("Snapshots for each game world. Select the world from the dropdown menu.")]
    public WorldSnapshot[] worldSnapshots = new WorldSnapshot[0];
    
    [Header("References")]
    [Tooltip("GameManager reference (auto-found if not assigned)")]
    public GameManager gameManager;
    
    private AudioMixerSnapshot currentSnapshot;
    private int currentWorldIndex = -1;
    
    void Start()
    {
        // Find GameManager if not assigned
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        
        if (gameManager == null)
        {
            Debug.LogWarning("AudioMixerSnapshotManager: GameManager not found! Snapshot transitions will not work.");
            return;
        }
        
        // Apply initial snapshot
        if (initialSnapshot != null)
        {
            currentSnapshot = initialSnapshot;
            initialSnapshot.TransitionTo(0f); // Instant transition at start
        }
        else
        {
            Debug.LogWarning("AudioMixerSnapshotManager: No initial snapshot assigned!");
        }
        
        // Subscribe to world changes by checking GameManager's world array
        // We'll need to manually call OnWorldChanged when worlds change
    }
    
    /// <summary>
    /// Called when a world change occurs. Should be called from GameManager or world change system.
    /// </summary>
    /// <param name="worldIndex">Index of the new world</param>
    /// <param name="worldName">Display name of the new world (for logging)</param>
    /// <param name="fadeDuration">Duration of the fade transition</param>
    public void OnWorldChanged(int worldIndex, string worldName, float fadeDuration)
    {
        Debug.Log($"AudioMixerSnapshotManager: OnWorldChanged called - World Index: {worldIndex}, Name: {worldName}, Fade Duration: {fadeDuration}");
        
        // Don't transition if it's the same world
        if (worldIndex == currentWorldIndex)
        {
            Debug.Log($"AudioMixerSnapshotManager: Same world index ({worldIndex}), skipping transition.");
            return;
        }
        
        // Find the snapshot for this world
        AudioMixerSnapshot targetSnapshot = FindSnapshotForWorld(worldIndex);
        
        // If no snapshot found for this world, keep current snapshot
        if (targetSnapshot == null)
        {
            Debug.LogWarning($"AudioMixerSnapshotManager: No snapshot found for world '{worldName}' (index {worldIndex}). Keeping current snapshot.");
            Debug.LogWarning($"AudioMixerSnapshotManager: Available world snapshots: {GetWorldSnapshotsDebugInfo()}");
            currentWorldIndex = worldIndex;
            return;
        }
        
        // If it's the same snapshot as current, no need to transition
        if (targetSnapshot == currentSnapshot)
        {
            Debug.Log($"AudioMixerSnapshotManager: Target snapshot is same as current, skipping transition.");
            currentWorldIndex = worldIndex;
            return;
        }
        
        // Transition to new snapshot
        Debug.Log($"AudioMixerSnapshotManager: Transitioning to snapshot '{targetSnapshot.name}' for world {worldIndex} ({worldName}) over {fadeDuration} seconds.");
        StartCoroutine(TransitionToSnapshot(targetSnapshot, fadeDuration));
        currentWorldIndex = worldIndex;
    }
    
    /// <summary>
    /// Finds the snapshot associated with a world index
    /// </summary>
    private AudioMixerSnapshot FindSnapshotForWorld(int worldIndex)
    {
        if (worldSnapshots == null || worldSnapshots.Length == 0)
        {
            Debug.LogWarning($"AudioMixerSnapshotManager: worldSnapshots array is null or empty!");
            return null;
        }
        
        Debug.Log($"AudioMixerSnapshotManager: Searching for snapshot with worldIndex {worldIndex}. Checking {worldSnapshots.Length} entries...");
        
        // Find snapshot by world index
        foreach (WorldSnapshot ws in worldSnapshots)
        {
            if (ws == null)
            {
                Debug.LogWarning("AudioMixerSnapshotManager: Found null WorldSnapshot entry in array!");
                continue;
            }
            
            Debug.Log($"AudioMixerSnapshotManager: Checking entry - worldIndex: {ws.worldIndex}, snapshot: {(ws.snapshot != null ? ws.snapshot.name : "NULL")}");
            
            if (ws.worldIndex == worldIndex && ws.snapshot != null)
            {
                Debug.Log($"AudioMixerSnapshotManager: Found matching snapshot '{ws.snapshot.name}' for world index {worldIndex}!");
                return ws.snapshot;
            }
        }
        
        Debug.LogWarning($"AudioMixerSnapshotManager: No matching snapshot found for world index {worldIndex}!");
        return null;
    }
    
    /// <summary>
    /// Gets debug information about configured world snapshots
    /// </summary>
    private string GetWorldSnapshotsDebugInfo()
    {
        if (worldSnapshots == null || worldSnapshots.Length == 0)
        {
            return "None configured";
        }
        
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < worldSnapshots.Length; i++)
        {
            if (worldSnapshots[i] != null)
            {
                sb.Append($"[{i}] worldIndex={worldSnapshots[i].worldIndex}, snapshot={(worldSnapshots[i].snapshot != null ? worldSnapshots[i].snapshot.name : "NULL")}; ");
            }
            else
            {
                sb.Append($"[{i}] NULL; ");
            }
        }
        return sb.ToString();
    }
    
    /// <summary>
    /// Transitions from current snapshot to target snapshot over the specified duration
    /// </summary>
    private IEnumerator TransitionToSnapshot(AudioMixerSnapshot targetSnapshot, float duration)
    {
        if (targetSnapshot == null)
        {
            yield break;
        }
        
        // If no current snapshot or instant transition, just transition to target
        if (currentSnapshot == null || duration <= 0f)
        {
            targetSnapshot.TransitionTo(0f);
            currentSnapshot = targetSnapshot;
            yield break;
        }
        
        // Fade out current snapshot and fade in new snapshot simultaneously
        // Unity's AudioMixerSnapshot.TransitionTo handles this automatically
        targetSnapshot.TransitionTo(duration);
        currentSnapshot = targetSnapshot;
        
        yield return new WaitForSeconds(duration);
    }
    
    /// <summary>
    /// Manually set a snapshot (useful for testing or special cases)
    /// </summary>
    public void SetSnapshot(AudioMixerSnapshot snapshot, float transitionTime = 0f)
    {
        if (snapshot == null)
        {
            Debug.LogWarning("AudioMixerSnapshotManager: Attempted to set null snapshot!");
            return;
        }
        
        snapshot.TransitionTo(transitionTime);
        currentSnapshot = snapshot;
    }
    
    /// <summary>
    /// Get the currently active snapshot
    /// </summary>
    public AudioMixerSnapshot GetCurrentSnapshot()
    {
        return currentSnapshot;
    }
    
    /// <summary>
    /// Reset to initial snapshot (useful for game restart)
    /// </summary>
    public void ResetToInitialSnapshot(float transitionTime = 0f)
    {
        if (initialSnapshot != null)
        {
            initialSnapshot.TransitionTo(transitionTime);
            currentSnapshot = initialSnapshot;
            currentWorldIndex = -1;
        }
    }
}
