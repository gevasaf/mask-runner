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
    
    [Header("Player Reference")]
    public Transform player;
    
    private int lives = 3;
    private int coins = 0;
    private float nextSpawnTime = 0f;
    private bool isGameOver = false;
    
    // UI Manager reference
    private UIManager uiManager;
    
    void Start()
    {
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
        if (enemyPrefab == null)
        {
            CreateEnemyPrefab();
        }
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
            
        // Spawn obstacles and coins
        if (Time.time >= nextSpawnTime)
        {
            SpawnObstacles();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }
    
    void SpawnObstacles()
    {
        float spawnZ = player != null ? player.position.z + spawnDistance : spawnDistance;
        
        // Randomly decide what to spawn
        int spawnType = Random.Range(0, 4);
        
        switch (spawnType)
        {
            case 0: // Spawn enemy
                SpawnEnemy(spawnZ);
                break;
            case 1: // Spawn coin
                SpawnCoin(spawnZ);
                break;
            case 2: // Spawn enemy and coin
                SpawnEnemy(spawnZ);
                SpawnCoin(spawnZ);
                break;
            case 3: // Spawn multiple coins
                for (int i = 0; i < 3; i++)
                {
                    SpawnCoin(spawnZ + i * 5f);
                }
                break;
        }
    }
    
    void SpawnEnemy(float zPos)
    {
        GameObject enemyObj;
        
        if (enemyPrefab != null)
        {
            enemyObj = Instantiate(enemyPrefab);
        }
        else
        {
            // Create a cube as enemy
            enemyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemyObj.AddComponent<Enemy>();
        }
        
        Enemy enemy = enemyObj.GetComponent<Enemy>();
        if (enemy == null)
        {
            enemy = enemyObj.AddComponent<Enemy>();
        }
        
        // Random lane (0, 1, or 2)
        int lane = Random.Range(0, 3);
        enemy.SetLane(lane);
        
        // Random position (Up, Down, or Both)
        int posType = Random.Range(0, 3);
        enemy.position = (Enemy.EnemyPosition)posType;
        
        // Random lane width (1 or 2)
        int width = Random.Range(1, 3);
        enemy.SetLaneWidth(width);
        
        // Set position and activate
        enemyObj.transform.position = new Vector3(enemyObj.transform.position.x, enemyObj.transform.position.y, zPos);
        enemyObj.SetActive(true);
        
        // Force setup after all properties are set
        enemy.SetupEnemy();
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
        
        // Destroy all existing enemies and coins
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
    }
    
    
    public void RestartGame()
    {
        Debug.Log("RestartGame called!");
        
        // Reset game state
        isGameOver = false;
        lives = 3;
        coins = 0;
        nextSpawnTime = Time.time + spawnInterval;
        
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
}
