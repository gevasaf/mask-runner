using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
    
    [Header("UI References")]
    public Text livesText;
    public Text coinsText;
    
    [Header("Player Reference")]
    public Transform player;
    
    private int lives = 3;
    private int coins = 0;
    private float nextSpawnTime = 0f;
    private bool isGameOver = false;
    
    // Game Over UI
    private GameObject gameOverPanel;
    private Text gameOverText;
    private Text scoreText;
    private Button restartButton;
    
    void Start()
    {
        // Create UI if not assigned
        if (livesText == null || coinsText == null)
        {
            CreateUI();
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
        
        // Create game over UI (but keep it hidden initially)
        CreateGameOverUI();
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        UpdateUI();
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
        UpdateUI();
        
        if (lives <= 0)
        {
            GameOver();
        }
    }
    
    public void CollectCoin()
    {
        coins++;
        UpdateUI();
    }
    
    void UpdateUI()
    {
        if (livesText != null)
        {
            livesText.text = "Lives: " + lives;
        }
        
        if (coinsText != null)
        {
            coinsText.text = "Coins: " + coins;
        }
    }
    
    void GameOver()
    {
        isGameOver = true;
        ShowGameOverUI();
        
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
    
    void CreateGameOverUI()
    {
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Ensure EventSystem exists for button clicks
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }
        
        // Create Game Over Panel
        if (gameOverPanel == null)
        {
            gameOverPanel = new GameObject("GameOverPanel");
            gameOverPanel.transform.SetParent(canvasObj.transform);
            
            Image panelImage = gameOverPanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f); // Semi-transparent black background
            
            RectTransform panelRect = gameOverPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;
        }
        
        // Create Game Over Text
        if (gameOverText == null)
        {
            GameObject gameOverObj = new GameObject("GameOverText");
            gameOverObj.transform.SetParent(gameOverPanel.transform);
            gameOverText = gameOverObj.AddComponent<Text>();
            gameOverText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            gameOverText.fontSize = 48;
            gameOverText.color = Color.white;
            gameOverText.text = "GAME OVER";
            gameOverText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform rectTransform = gameOverObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0, 100);
            rectTransform.sizeDelta = new Vector2(400, 60);
        }
        
        // Create Score Text
        if (scoreText == null)
        {
            GameObject scoreObj = new GameObject("ScoreText");
            scoreObj.transform.SetParent(gameOverPanel.transform);
            scoreText = scoreObj.AddComponent<Text>();
            scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            scoreText.fontSize = 36;
            scoreText.color = Color.yellow;
            scoreText.text = "Score: " + coins;
            scoreText.alignment = TextAnchor.MiddleCenter;
            
            RectTransform rectTransform = scoreObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = new Vector2(0, 20);
            rectTransform.sizeDelta = new Vector2(400, 50);
        }
        else
        {
            scoreText.text = "Score: " + coins;
        }
        
        // Create Restart Button
        if (restartButton == null)
        {
            GameObject buttonObj = new GameObject("RestartButton");
            buttonObj.transform.SetParent(gameOverPanel.transform);
            
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 0.2f); // Green color
            
            restartButton = buttonObj.AddComponent<Button>();
            
            // Button text
            GameObject buttonTextObj = new GameObject("Text");
            buttonTextObj.transform.SetParent(buttonObj.transform);
            Text buttonText = buttonTextObj.AddComponent<Text>();
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 32;
            buttonText.color = Color.white;
            buttonText.text = "RESTART";
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.raycastTarget = false; // Don't block button clicks
            
            RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.sizeDelta = Vector2.zero;
            buttonTextRect.anchoredPosition = Vector2.zero;
            
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(0, -80);
            buttonRect.sizeDelta = new Vector2(200, 60);
            
            // Add button listener
            restartButton.onClick.AddListener(RestartGame);
        }
    }
    
    void ShowGameOverUI()
    {
        // Create UI if it doesn't exist
        if (gameOverPanel == null)
        {
            CreateGameOverUI();
        }
        
        // Update score text
        if (scoreText != null)
        {
            scoreText.text = "Score: " + coins;
        }
        
        // Show the panel
        gameOverPanel.SetActive(true);
    }
    
    void HideGameOverUI()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
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
        
        // Hide game over UI
        HideGameOverUI();
        
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
        
        // Update UI
        UpdateUI();
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
    
    void CreateUI()
    {
        // Create Canvas
        GameObject canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Ensure EventSystem exists for button clicks
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }
        
        // Create Lives Text
        if (livesText == null)
        {
            GameObject livesObj = new GameObject("LivesText");
            livesObj.transform.SetParent(canvasObj.transform);
            livesText = livesObj.AddComponent<Text>();
            livesText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            livesText.fontSize = 24;
            livesText.color = Color.white;
            livesText.text = "Lives: 3";
            
            RectTransform rectTransform = livesObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(10, -10);
            rectTransform.sizeDelta = new Vector2(200, 30);
        }
        
        // Create Coins Text
        if (coinsText == null)
        {
            GameObject coinsObj = new GameObject("CoinsText");
            coinsObj.transform.SetParent(canvasObj.transform);
            coinsText = coinsObj.AddComponent<Text>();
            coinsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            coinsText.fontSize = 24;
            coinsText.color = Color.white;
            coinsText.text = "Coins: 0";
            
            RectTransform rectTransform = coinsObj.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(10, -50);
            rectTransform.sizeDelta = new Vector2(200, 30);
        }
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
