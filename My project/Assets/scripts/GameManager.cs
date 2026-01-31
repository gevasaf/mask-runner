using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Game Settings")]
    public float forwardSpeed = 10f;
    public float spawnDistance = 50f;
    public float spawnInterval = 2f;
    public float laneWidth = 2f;
    
    [Header("Spawn Timing")]
    [Tooltip("Initial delay before spawning starts (in seconds). Coins/power-ups spawn after this; enemies after this + one spawn interval.")]
    public float initialSpawnDelay = 5f;
    
    [Header("Prefabs")]
    public GameObject coinPrefab;
    [Tooltip("Power-up prefabs: [0] MilkCup, [1] ChocoCup, [2] Bandage. Put prefabs in Assets/prefabs/.")]
    public GameObject[] powerUpPrefabs = new GameObject[3];
    [Tooltip("Particle blast prefab (must have ParticleBlast + ParticleSystem). Used for collect/hit feedback.")]
    public GameObject particleBlastPrefab;
    
    [Header("Player Reference")]
    public Transform player;
    
    [Header("Power-Up Settings")]
    [Tooltip("Chance to spawn a power-up item (0.0 to 1.0)")]
    [Range(0f, 1f)]
    public float powerUpSpawnChance = 0.1f;
    
    [Header("Developer Settings")]
    [Tooltip("Enable developer shortcuts (Z=MilkCup, X=ChocoCup, C=Bandage)")]
    public bool enableDeveloperShortcuts = true;
    
    [Header("Worlds")]
    [Tooltip("List of worlds. First world is used at start; every 40 seconds a random world (except first) is chosen.")]
    public WorldData[] worlds;
    [Tooltip("Interval in seconds between world changes.")]
    public float worldChangeInterval = 40f;
    [Tooltip("Delay in seconds after stopping spawner before switching world (blackout).")]
    public float worldTransitionDelay = 5f;
    [Tooltip("Duration of floor/skybox fade to new world.")]
    public float worldFadeDuration = 5f;
    
    [Header("World References")]
    public Floor floor;
    public SkyboxController skyboxController;
    public EnemySpawner enemySpawner;
    
    private int lives = 3;
    private int coins = 0;
    private float nextSpawnTime = 0f;
    private bool isGameOver = false;
    
    // Power-up state
    private float originalForwardSpeed;
    private bool isSpeedBoostActive = false;
    private bool isInvincible = false;
    
    // World state
    private int currentWorldIndex = 0;
    private float nextWorldChangeTime = 0f;
    private bool isWorldTransitioning = false;
    
    // UI Manager reference
    private UIManager uiManager;
    
    void Start()
    {
        // Store original forward speed
        originalForwardSpeed = forwardSpeed;
        
        // Find or create UI Manager
        uiManager = UIManager.Instance;
        if (uiManager == null)
        {
            GameObject uiManagerObj = new GameObject("UIManager");
            uiManager = uiManagerObj.AddComponent<UIManager>();
        }
        
        // Find player if not assigned
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
        
        // Create prefabs if they don't exist
        if (coinPrefab == null)
        {
            CreateCoinPrefab();
        }
        
        // Initialize spawn timer with initial delay (prevents coins, power-ups, and enemies from spawning)
        nextSpawnTime = Time.time + initialSpawnDelay;
        
        // Apply first world
        if (worlds != null && worlds.Length > 0)
        {
            currentWorldIndex = 0;
            ApplyWorld(0);
            nextWorldChangeTime = Time.time + worldChangeInterval;
        }
        
        // Initialize UI
        if (uiManager != null)
        {
            uiManager.UpdateUI(lives, coins);
        }
    }
    
    void Update()
    {
        if (isGameOver)
            return;
        
        // Developer shortcuts for spawning power-ups
        if (enableDeveloperShortcuts)
        {
            HandleDeveloperShortcuts();
        }
            
        // Spawn obstacles and coins
        if (Time.time >= nextSpawnTime)
        {
            SpawnObstacles();
            nextSpawnTime = Time.time + spawnInterval;
        }
        
        // World change timer (every 40 seconds, pick random world except first)
        if (!isWorldTransitioning && worlds != null && worlds.Length > 1 && Time.time >= nextWorldChangeTime)
        {
            StartCoroutine(WorldTransitionCoroutine());
        }
    }
    
    /// <summary>
    /// Build opacities for a world by index: 1 at that index, 0 for the rest (max 6 layers).
    /// </summary>
    static float[] OpacitiesFromWorldIndex(int worldIndex)
    {
        float[] o = new float[6];
        if (worldIndex >= 0 && worldIndex < 6)
            o[worldIndex] = 1f;
        return o;
    }

    /// <summary>
    /// Apply a world immediately (floor/skybox opacities from world index, spawner prefabs).
    /// </summary>
    void ApplyWorld(int worldIndex)
    {
        if (worlds == null || worldIndex < 0 || worldIndex >= worlds.Length)
            return;
        WorldData w = worlds[worldIndex];
        if (w == null)
            return;
        float[] opacities = OpacitiesFromWorldIndex(worldIndex);
        if (floor != null)
            floor.SetOpacities(opacities);
        if (skyboxController != null)
            skyboxController.SetOpacities(opacities);
        if (enemySpawner != null)
            enemySpawner.SetEnemyPrefabs(w.enemyPrefabs);
    }
    
    IEnumerator WorldTransitionCoroutine()
    {
        isWorldTransitioning = true;
        
        if (enemySpawner != null)
            enemySpawner.SetSpawningEnabled(false);
        
        yield return new WaitForSeconds(worldTransitionDelay);
        
        // Pick random world (excluding first and current)
        int[] valid = new int[worlds.Length];
        int validCount = 0;
        for (int i = 1; i < worlds.Length; i++)
        {
            if (i != currentWorldIndex && worlds[i] != null)
                valid[validCount++] = i;
        }
        if (validCount == 0)
        {
            isWorldTransitioning = false;
            if (enemySpawner != null)
                enemySpawner.SetSpawningEnabled(true);
            nextWorldChangeTime = Time.time + worldChangeInterval;
            yield break;
        }
        int nextIndex = valid[Random.Range(0, validCount)];
        WorldData nextWorld = worlds[nextIndex];
        if (nextWorld == null)
        {
            isWorldTransitioning = false;
            if (enemySpawner != null)
                enemySpawner.SetSpawningEnabled(true);
            nextWorldChangeTime = Time.time + worldChangeInterval;
            yield break;
        }
        
        // 1. Start fade over 5 seconds (non-blocking)
        StartCoroutine(FadeToWorldCoroutine(nextIndex));
        
        // 2. Show world title (like power-up)
        if (uiManager != null)
            uiManager.ShowWorldTitle(nextIndex, 2f);
        
        // 3. Set spawner to new world and turn back on
        if (enemySpawner != null)
        {
            enemySpawner.SetEnemyPrefabs(nextWorld.enemyPrefabs);
            enemySpawner.IncreaseSpawnRate(); // Increase spawn rate for each new world
            enemySpawner.SetSpawningEnabled(true);
        }
        
        currentWorldIndex = nextIndex;
        nextWorldChangeTime = Time.time + worldChangeInterval;
        isWorldTransitioning = false;
    }
    
    IEnumerator FadeToWorldCoroutine(int targetWorldIndex)
    {
        if (worlds == null || targetWorldIndex < 0 || targetWorldIndex >= worlds.Length)
            yield break;
        if (worlds[targetWorldIndex] == null)
            yield break;
        
        float[] floorStart = floor != null ? floor.GetOpacities() : new float[6];
        float[] floorEnd = OpacitiesFromWorldIndex(targetWorldIndex);
        float[] skyboxStart = skyboxController != null ? skyboxController.GetOpacities() : new float[6];
        float[] skyboxEnd = OpacitiesFromWorldIndex(targetWorldIndex);
        
        float elapsed = 0f;
        while (elapsed < worldFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / worldFadeDuration);
            float[] floorCur = new float[6];
            float[] skyboxCur = new float[6];
            for (int i = 0; i < 6; i++)
            {
                floorCur[i] = Mathf.Lerp(floorStart[i], floorEnd[i], t);
                skyboxCur[i] = Mathf.Lerp(skyboxStart[i], skyboxEnd[i], t);
            }
            if (floor != null)
                floor.SetOpacities(floorCur);
            if (skyboxController != null)
                skyboxController.SetOpacities(skyboxCur);
            yield return null;
        }
        if (floor != null)
            floor.SetOpacities(floorEnd);
        if (skyboxController != null)
            skyboxController.SetOpacities(skyboxEnd);
    }
    
    /// <summary>
    /// Handle developer keyboard shortcuts for spawning power-ups
    /// </summary>
    void HandleDeveloperShortcuts()
    {
        float spawnZ = player != null ? player.position.z + 10f : 10f; // Spawn slightly ahead of player
        
        if (Input.GetKeyDown(KeyCode.Z))
        {
            // Spawn MilkCup
            SpawnPowerUpItem(PowerUpItem.ItemType.MilkCup, spawnZ);
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            // Spawn ChocoCup
            SpawnPowerUpItem(PowerUpItem.ItemType.ChocoCup, spawnZ);
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            // Spawn Bandage
            SpawnPowerUpItem(PowerUpItem.ItemType.Bandage, spawnZ);
        }
    }
    
    void SpawnObstacles()
    {
        float spawnZ = player != null ? player.position.z + spawnDistance : spawnDistance;
        
        // Try to spawn a power-up item (rare)
        SpawnItem(spawnZ);
        
        // Randomly decide what to spawn
        // Note: Enemy spawning is now handled by EnemySpawner, so we only spawn coins here
        int spawnType = Random.Range(0, 3);
        
        switch (spawnType)
        {
            case 0: // Spawn coin
                SpawnCoin(spawnZ);
                break;
            case 1: // Spawn multiple coins
                for (int i = 0; i < 3; i++)
                {
                    SpawnCoin(spawnZ + i * 5f);
                }
                break;
            case 2: // Spawn coin at different positions
                SpawnCoin(spawnZ);
                SpawnCoin(spawnZ + 5f);
                break;
        }
    }
    
    void SpawnCoin(float zPos)
    {
        GameObject coinObj;
        
        if (coinPrefab != null)
        {
            coinObj = Instantiate(coinPrefab);
            coinObj.SetActive(true);
        }
        else
        {
            // Create a sphere as coin
            coinObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            coinObj.AddComponent<Coin>();
        }
        
        Coin coin = coinObj.GetComponent<Coin>();
        if (coin == null)
        {
            coin = coinObj.AddComponent<Coin>();
        }
        
        // Random lane
        int lane = Random.Range(0, 3);
        float xPos = (lane - 1) * laneWidth; // Calculate x position directly
        
        // Random height (ground level or slightly above)
        float yPos = Random.Range(0f, 1.5f);
        
        // Set position directly (more reliable than using SetLane)
        coinObj.transform.position = new Vector3(xPos, yPos, zPos);
    }
    
    public void PlayerHit()
    {
        if (isGameOver)
            return;
        
        // Don't take damage if invincible
        if (isInvincible)
            return;
            
        lives--;
        
        // Update UI through UIManager
        if (uiManager != null)
        {
            uiManager.UpdateLives(lives);
        }
        
        if (lives <= 0)
        {
            GameOver();
        }
    }
    
    public void CollectCoin()
    {
        coins++;
        
        // Update UI through UIManager
        if (uiManager != null)
        {
            uiManager.UpdateCoins(coins);
        }
    }
    
    void GameOver()
    {
        isGameOver = true;
        
        // Show game over UI through UIManager
        if (uiManager != null)
        {
            uiManager.ShowGameOver(coins);
        }
        
        // Stop player input and disable player GameObject
        Player playerScript = FindObjectOfType<Player>();
        if (playerScript != null)
        {
            playerScript.SetGameOver(true);
        }
        
        // Disable player GameObject so it's not visible
        if (player != null)
        {
            player.gameObject.SetActive(false);
        }
        
        // Destroy all existing enemies, coins, and power-ups
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }
        
        GameObject[] coinObjects = GameObject.FindGameObjectsWithTag("Coin");
        foreach (GameObject coin in coinObjects)
        {
            Destroy(coin);
        }
        
        // Destroy all power-up items
        PowerUpItem[] powerUps = FindObjectsOfType<PowerUpItem>();
        foreach (PowerUpItem powerUp in powerUps)
        {
            Destroy(powerUp.gameObject);
        }
    }
    
    
    public void RestartGame()
    {
        Debug.Log("RestartGame called!");
        
        // Stop any active power-up coroutines
        if (isSpeedBoostActive)
        {
            StopCoroutine("SpeedBoostCoroutine");
        }
        
        // Reset game state
        isGameOver = false;
        lives = 3;
        coins = 0;
        nextSpawnTime = Time.time + initialSpawnDelay + spawnInterval;
        
        // Reset power-up states
        forwardSpeed = originalForwardSpeed;
        isSpeedBoostActive = false;
        isInvincible = false;
        
        // Reset to first world
        if (worlds != null && worlds.Length > 0)
        {
            currentWorldIndex = 0;
            ApplyWorld(0);
            nextWorldChangeTime = Time.time + worldChangeInterval;
            isWorldTransitioning = false;
            if (enemySpawner != null)
                enemySpawner.SetSpawningEnabled(true);
        }
        
        // Hide game over UI through UIManager
        if (uiManager != null)
        {
            uiManager.HideGameOver();
            uiManager.HidePause(); // Ensure pause is also cleared on restart
            uiManager.UpdateUI(lives, coins);
        }
        
        // Reset player and re-enable player GameObject
        Player playerScript = FindObjectOfType<Player>();
        if (playerScript != null)
        {
            playerScript.SetGameOver(false);
        }
        
        // Re-enable player GameObject so it's visible again
        if (player != null)
        {
            player.gameObject.SetActive(true);
            // Reset player position
            player.position = new Vector3(0, player.position.y, player.position.z);
        }
        else
        {
            // If player reference is lost, try to find it again
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                player.gameObject.SetActive(true);
                player.position = new Vector3(0, player.position.y, player.position.z);
            }
        }
    }
    
    public float GetForwardSpeed()
    {
        // Return 0 if game is over to stop all movement
        return isGameOver ? 0f : forwardSpeed;
    }
    
    public float GetSpawnDistance()
    {
        return spawnDistance;
    }
    
    public float GetSpawnInterval()
    {
        return spawnInterval;
    }
    
    public float GetInitialSpawnDelay()
    {
        return initialSpawnDelay;
    }
    
    public bool IsGameOver()
    {
        return isGameOver;
    }
    
    public int GetLives()
    {
        return lives;
    }
    
    public int GetCoins()
    {
        return coins;
    }
    
    void CreateCoinPrefab()
    {
        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        coin.name = "CoinPrefab";
        coin.AddComponent<Coin>();
        coin.tag = "Coin";
        coin.transform.localScale = Vector3.one * 0.5f;
        coin.SetActive(false);
        coinPrefab = coin;
    }
    
    // Power-Up Methods
    
    /// <summary>
    /// Spawns a specific power-up item at the given position
    /// </summary>
    public void SpawnPowerUpItem(PowerUpItem.ItemType itemType, float zPos)
    {
        GameObject powerUpObj;
        int typeIndex = (int)itemType;
        GameObject prefab = (powerUpPrefabs != null && typeIndex >= 0 && typeIndex < powerUpPrefabs.Length)
            ? powerUpPrefabs[typeIndex]
            : null;

        if (prefab != null)
        {
            powerUpObj = Instantiate(prefab);
            powerUpObj.SetActive(true);
        }
        else
        {
            // Fallback: create a sphere as power-up item
            powerUpObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            powerUpObj.AddComponent<PowerUpItem>();
        }

        PowerUpItem powerUp = powerUpObj.GetComponent<PowerUpItem>();
        if (powerUp == null)
        {
            powerUp = powerUpObj.AddComponent<PowerUpItem>();
        }

        powerUp.itemType = itemType;

        // Random lane
        int lane = Random.Range(0, 3);
        float xPos = (lane - 1) * laneWidth;

        // Random position (Up or Down only)
        int posType = Random.Range(0, 2);
        powerUp.position = (PowerUpItem.PowerUpPosition)posType;

        // Set initial position (will be adjusted by SetupPowerUp)
        powerUpObj.transform.position = new Vector3(xPos, 0f, zPos);

        // Setup power-up (this will set the correct Y position based on position type)
        powerUp.SetupPowerUp();
    }
    
    /// <summary>
    /// Spawns a power-up item with smart probability based on player's current lives
    /// </summary>
    public void SpawnItem(float zPos)
    {
        // Check if we should spawn a power-up based on spawn chance
        if (Random.Range(0f, 1f) > powerUpSpawnChance)
            return;
        
        // Smart item selection based on lives
        PowerUpItem.ItemType selectedType;
        
        if (lives >= 5)
        {
            // At max lives, can't get bandage - choose between MilkCup and ChocoCup
            selectedType = Random.Range(0, 2) == 0 ? PowerUpItem.ItemType.MilkCup : PowerUpItem.ItemType.ChocoCup;
        }
        else if (lives == 1)
        {
            // At 1 life, much more likely to get bandage (70% chance)
            float rand = Random.Range(0f, 1f);
            if (rand < 0.7f)
            {
                selectedType = PowerUpItem.ItemType.Bandage;
            }
            else if (rand < 0.85f)
            {
                selectedType = PowerUpItem.ItemType.MilkCup;
            }
            else
            {
                selectedType = PowerUpItem.ItemType.ChocoCup;
            }
        }
        else
        {
            // Normal distribution: 40% MilkCup, 30% ChocoCup, 30% Bandage
            float rand = Random.Range(0f, 1f);
            if (rand < 0.4f)
            {
                selectedType = PowerUpItem.ItemType.MilkCup;
            }
            else if (rand < 0.7f)
            {
                selectedType = PowerUpItem.ItemType.ChocoCup;
            }
            else
            {
                selectedType = PowerUpItem.ItemType.Bandage;
            }
        }
        
        // Spawn the selected power-up type
        SpawnPowerUpItem(selectedType, zPos);
    }
    
    /// <summary>
    /// Adds lives to the player
    /// </summary>
    public void AddLives(int amount)
    {
        lives += amount;
        
        // Cap lives at 5
        if (lives > 5)
            lives = 5;
        
        // Update UI
        if (uiManager != null)
        {
            uiManager.UpdateLives(lives);
        }
    }
    
    /// <summary>
    /// Adds coins to the player's score
    /// </summary>
    public void AddCoins(int amount)
    {
        coins += amount;
        
        // Update UI
        if (uiManager != null)
        {
            uiManager.UpdateCoins(coins);
        }
    }
    
    /// <summary>
    /// Activates a speed boost with multiplier and duration
    /// </summary>
    public void ActivateSpeedBoost(float multiplier, float duration)
    {
        // Stop any existing speed boost coroutine
        if (isSpeedBoostActive)
        {
            StopCoroutine("SpeedBoostCoroutine");
        }
        
        StartCoroutine(SpeedBoostCoroutine(multiplier, duration));
    }
    
    /// <summary>
    /// Coroutine that handles speed boost duration and invincibility
    /// </summary>
    private IEnumerator SpeedBoostCoroutine(float multiplier, float duration)
    {
        isSpeedBoostActive = true;
        isInvincible = true;
        
        // Apply speed boost
        forwardSpeed = originalForwardSpeed * multiplier;
        
        // Wait for duration
        yield return new WaitForSeconds(duration);
        
        // Restore original speed
        forwardSpeed = originalForwardSpeed;
        isSpeedBoostActive = false;
        isInvincible = false;
    }
    
    /// <summary>
    /// Check if player is currently invincible
    /// </summary>
    public bool IsInvincible()
    {
        return isInvincible;
    }
    
    /// <summary>
    /// Check if speed boost (ChocoCup) is currently active
    /// </summary>
    public bool IsSpeedBoostActive()
    {
        return isSpeedBoostActive;
    }

    /// <summary>
    /// Spawn a one-shot particle blast at the given position with size and color.
    /// Requires particleBlastPrefab to be assigned (prefab with ParticleBlast + ParticleSystem).
    /// </summary>
    public void SpawnParticleBlast(Vector3 position, float size, Color color)
    {
        if (particleBlastPrefab == null)
            return;
        GameObject go = Instantiate(particleBlastPrefab, position, Quaternion.identity);
        ParticleBlast pb = go.GetComponent<ParticleBlast>();
        if (pb != null)
            pb.Initialize(size, color);
        else
            Destroy(go);
    }
}
