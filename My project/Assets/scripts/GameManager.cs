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
    
    [Header("Prefabs")]
    public GameObject enemyPrefab;
    public GameObject coinPrefab;
    public GameObject powerUpPrefab;
    
    [Header("Player Reference")]
    public Transform player;
    
    [Header("Power-Up Settings")]
    [Tooltip("Chance to spawn a power-up item (0.0 to 1.0)")]
    [Range(0f, 1f)]
    public float powerUpSpawnChance = 0.1f;
    
    [Header("Developer Settings")]
    [Tooltip("Enable developer shortcuts (Z=MilkCup, X=ChocoCup, C=Bandage)")]
    public bool enableDeveloperShortcuts = true;
    
    private int lives = 3;
    private int coins = 0;
    private float nextSpawnTime = 0f;
    private bool isGameOver = false;
    
    // Power-up state
    private float originalForwardSpeed;
    private bool isSpeedBoostActive = false;
    private bool isInvincible = false;
    
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
        // Note: enemyPrefab is deprecated - enemies are now spawned by EnemySpawner
        if (coinPrefab == null)
        {
            CreateCoinPrefab();
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
    
    void SpawnEnemy(float zPos)
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("GameManager: enemyPrefab is null. Cannot spawn enemy.");
            return;
        }
        
        // Instantiate enemy from prefab
        GameObject enemyObj = Instantiate(enemyPrefab);
        
        // Get enemy component to check laneWidth
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy == null)
        {
            Debug.LogWarning("GameManager: enemyPrefab does not have an Enemy component.");
            Destroy(enemyObj);
            return;
        }
        
        // Calculate spawn X position based on laneWidth using smart lane alignment
        float spawnX = CalculateEnemySpawnX(enemy.laneWidth);
        
        // Set position and activate
        enemyObj.transform.position = new Vector3(spawnX, enemyObj.transform.position.y, zPos);
        enemyObj.SetActive(true);
    }
    
    /// <summary>
    /// Calculates the X position for spawning an enemy based on laneWidth
    /// laneWidth 1: spawns on one of the three lanes (-2, 0, 2)
    /// laneWidth 2: spawns between two lanes (-1 or 1) to block both paths
    /// </summary>
    float CalculateEnemySpawnX(int laneWidth)
    {
        float[] lanes = { -2f, 0f, 2f };
        
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
            Debug.LogWarning("GameManager: Invalid laneWidth " + laneWidth + ", defaulting to center lane");
            return lanes[1]; // Center lane
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
        
        // Stop player input
        Player playerScript = FindObjectOfType<Player>();
        if (playerScript != null)
        {
            playerScript.SetGameOver(true);
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
        nextSpawnTime = Time.time + spawnInterval;
        
        // Reset power-up states
        forwardSpeed = originalForwardSpeed;
        isSpeedBoostActive = false;
        isInvincible = false;
        
        // Hide game over UI through UIManager
        if (uiManager != null)
        {
            uiManager.HideGameOver();
            uiManager.HidePause(); // Ensure pause is also cleared on restart
            uiManager.UpdateUI(lives, coins);
        }
        
        // Reset player
        Player playerScript = FindObjectOfType<Player>();
        if (playerScript != null)
        {
            playerScript.SetGameOver(false);
            // Reset player position
            if (player != null)
            {
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
    
    void CreateEnemyPrefab()
    {
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        enemy.name = "EnemyPrefab";
        enemy.AddComponent<Enemy>();
        enemy.tag = "Enemy";
        enemy.SetActive(false);
        enemyPrefab = enemy;
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
        
        if (powerUpPrefab != null)
        {
            powerUpObj = Instantiate(powerUpPrefab);
            powerUpObj.SetActive(true);
        }
        else
        {
            // Create a sphere as power-up item
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
        
        // Set visual color based on type
        Renderer renderer = powerUpObj.GetComponent<Renderer>();
        if (renderer != null)
        {
            switch (itemType)
            {
                case PowerUpItem.ItemType.MilkCup:
                    renderer.material.color = Color.white;
                    break;
                case PowerUpItem.ItemType.ChocoCup:
                    renderer.material.color = new Color(0.4f, 0.2f, 0.1f); // Brown
                    break;
                case PowerUpItem.ItemType.Bandage:
                    renderer.material.color = new Color(1f, 0.5f, 0f); // Orange
                    break;
            }
        }
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
}
