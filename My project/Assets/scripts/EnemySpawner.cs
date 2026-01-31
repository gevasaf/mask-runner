using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefabs (fallback / first world)")]
    [Tooltip("Default enemy prefabs if not set per-world. Each world can override via SetEnemyPrefabs.")]
    public GameObject[] enemyPrefabs;
    
    [Header("Lane Configuration")]
    [Tooltip("Fixed lane X positions (left, center, right)")]
    private readonly float[] lanes = { -2f, 0f, 2f };
    
    [Header("Spawn Rate Scaling")]
    [Tooltip("Percentage increase in spawn rate per world change (0.3 = 30% faster spawning). This value is cumulative - each world change makes enemies spawn this much faster.")]
    [Range(0f, 1f)]
    public float spawnRateIncreasePerWorld = 0.3f;
    
    private Transform player;
    private GameManager gameManager;
    private float nextSpawnTime = 0f;
    private float spawnDistance;
    private float baseSpawnInterval;
    private float currentSpawnInterval;
    private float spawnRateMultiplier = 1f;
    private GameObject[] _activePrefabs;
    private bool _spawningEnabled = true;
    
    void Start()
    {
        // Find player reference
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        
        // Find GameManager reference
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("EnemySpawner: GameManager not found! Cannot get spawn settings.");
            return;
        }
        
        // Get spawn settings from GameManager
        spawnDistance = gameManager.GetSpawnDistance();
        baseSpawnInterval = gameManager.GetSpawnInterval();
        currentSpawnInterval = baseSpawnInterval;
        float initialDelay = gameManager.GetInitialSpawnDelay();
        
        _activePrefabs = enemyPrefabs != null && enemyPrefabs.Length > 0 ? enemyPrefabs : null;
        ValidateEnemyPrefabs();
        
        // Initialize spawn timer with initial delay
        nextSpawnTime = Time.time + initialDelay + currentSpawnInterval;
    }
    
    /// <summary>
    /// Set the list of enemy prefabs for the current world (called by GameManager on world change).
    /// </summary>
    public void SetEnemyPrefabs(GameObject[] prefabs)
    {
        _activePrefabs = prefabs != null && prefabs.Length > 0 ? prefabs : enemyPrefabs;
    }
    
    /// <summary>
    /// Enable or disable spawning (e.g. during world transition).
    /// </summary>
    public void SetSpawningEnabled(bool enabled)
    {
        _spawningEnabled = enabled;
    }
    
    /// <summary>
    /// Increases the enemy spawn rate by the configured percentage. Called when player reaches a new world.
    /// Each call makes enemies spawn faster (reduces spawn interval).
    /// </summary>
    public void IncreaseSpawnRate()
    {
        // Increase the spawn rate multiplier (e.g., 1.0 -> 1.3 -> 1.6 -> 1.9...)
        spawnRateMultiplier += spawnRateIncreasePerWorld;
        
        // Calculate new spawn interval: base interval divided by multiplier
        // This makes spawning faster (smaller interval = more frequent spawning)
        currentSpawnInterval = baseSpawnInterval / spawnRateMultiplier;
        
        // Ensure spawn interval doesn't go below a minimum threshold (e.g., 0.1 seconds)
        if (currentSpawnInterval < 0.1f)
        {
            currentSpawnInterval = 0.1f;
        }
        
        Debug.Log($"EnemySpawner: Spawn rate increased! New spawn interval: {currentSpawnInterval:F2}s (Multiplier: {spawnRateMultiplier:F2}x)");
    }
    
    /// <summary>
    /// Validates that all enemy prefabs have the Enemy component configured
    /// </summary>
    void ValidateEnemyPrefabs()
    {
        GameObject[] toValidate = _activePrefabs != null ? _activePrefabs : enemyPrefabs;
        if (toValidate == null || toValidate.Length == 0)
        {
            Debug.LogWarning("EnemySpawner: No enemy prefabs assigned! Enemies will not spawn.");
            return;
        }
        
        for (int i = 0; i < toValidate.Length; i++)
        {
            if (toValidate[i] == null)
            {
                Debug.LogWarning($"EnemySpawner: Enemy prefab at index {i} is null!");
                continue;
            }
            
            Enemy enemy = toValidate[i].GetComponent<Enemy>();
            if (enemy == null)
            {
                Debug.LogWarning($"EnemySpawner: Prefab '{toValidate[i].name}' at index {i} does not have an Enemy component!");
            }
            else
            {
                if (enemy.laneWidth < 1 || enemy.laneWidth > 2)
                {
                    Debug.LogWarning($"EnemySpawner: Prefab '{toValidate[i].name}' has invalid laneWidth ({enemy.laneWidth}). Should be 1 or 2.");
                }
            }
        }
    }
    
    void Update()
    {
        if (!_spawningEnabled)
            return;
        // Don't spawn if game is over or no prefabs available
        if (gameManager != null && gameManager.IsGameOver())
            return;
        
        GameObject[] prefabs = _activePrefabs != null ? _activePrefabs : enemyPrefabs;
        if (prefabs == null || prefabs.Length == 0)
            return;
        
        // Spawn enemy at interval
        if (Time.time >= nextSpawnTime)
        {
            SpawnEnemy();
            nextSpawnTime = Time.time + currentSpawnInterval;
        }
    }
    
    /// <summary>
    /// Spawns a random enemy from the prefab array with smart lane alignment
    /// </summary>
    void SpawnEnemy()
    {
        GameObject[] prefabs = _activePrefabs != null ? _activePrefabs : enemyPrefabs;
        if (prefabs == null || prefabs.Length == 0)
            return;
        int prefabIndex = Random.Range(0, prefabs.Length);
        GameObject selectedPrefab = prefabs[prefabIndex];
        
        if (selectedPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: Selected prefab is null at index " + prefabIndex);
            return;
        }
        
        // Get enemy component to check laneWidth
        Enemy enemyComponent = selectedPrefab.GetComponent<Enemy>();
        if (enemyComponent == null)
        {
            Debug.LogWarning("EnemySpawner: Selected prefab does not have an Enemy component");
            return;
        }
        
        // Calculate spawn position
        float spawnZ = player != null ? player.position.z + spawnDistance : spawnDistance;
        float spawnX = CalculateSpawnX(enemyComponent.laneWidth);
        
        // Preserve the prefab's Y position (for enemies at different heights)
        float spawnY = selectedPrefab.transform.position.y;
        
        // Instantiate enemy
        GameObject enemyInstance = Instantiate(selectedPrefab, new Vector3(spawnX, spawnY, spawnZ), Quaternion.identity);
        
        // Ensure enemy has proper tag for collision detection
        if (!enemyInstance.CompareTag("Enemy"))
        {
            enemyInstance.tag = "Enemy";
        }
        
        // Ensure enemy has a collider (check if prefab has one, if not add a basic one)
        Collider collider = enemyInstance.GetComponent<Collider>();
        if (collider == null)
        {
            // Add a box collider if none exists
            BoxCollider boxCollider = enemyInstance.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            Debug.LogWarning($"EnemySpawner: Added BoxCollider to '{enemyInstance.name}' as it was missing a collider. Please add a collider to the prefab.");
        }
        else
        {
            // Ensure collider is a trigger
            collider.isTrigger = true;
        }
        
        enemyInstance.SetActive(true);
    }
    
    /// <summary>
    /// Calculates the X position for spawning based on laneWidth
    /// laneWidth 1: spawns on one of the three lanes (-2, 0, 2)
    /// laneWidth 2: spawns between two lanes (-1 or 1) to block both paths
    /// </summary>
    float CalculateSpawnX(int laneWidth)
    {
        if (laneWidth == 1)
        {
            // Spawn on one of the three fixed lanes
            int laneIndex = Random.Range(0, lanes.Length);
            return lanes[laneIndex];
        }
        else if (laneWidth == 2)
        {
            // Spawn between two lanes
            // Options: between lane 0 and 1 (x = -1), or between lane 1 and 2 (x = 1)
            int choice = Random.Range(0, 2);
            return choice == 0 ? -1f : 1f;
        }
        else
        {
            // Fallback: default to center lane
            Debug.LogWarning("EnemySpawner: Invalid laneWidth " + laneWidth + ", defaulting to center lane");
            return lanes[1]; // Center lane
        }
    }
}
