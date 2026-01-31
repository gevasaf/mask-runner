using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI Panel References")]
    [Tooltip("Drag the Canvas GameObject here, or leave null to find automatically")]
    public Canvas canvas;
    
    [Tooltip("Drag the HUD panel here, or leave null to find automatically")]
    public GameObject hudPanel;
    
    [Tooltip("Drag the Health Panel here, or leave null to find automatically")]
    public GameObject healthPanel;
    
    [Tooltip("Drag the Score Panel here, or leave null to find automatically")]
    public GameObject scorePanel;
    
    [Tooltip("Drag the Game Over Panel here, or leave null to find automatically")]
    public GameObject gameOverPanel;
    
    [Tooltip("Drag the Game Pause Panel here, or leave null to find automatically")]
    public GameObject gamePausePanel;
    
    [Header("UI Text References")]
    [Tooltip("TextMeshPro component for displaying lives/health")]
    public TextMeshProUGUI livesText;
    
    [Tooltip("TextMeshPro component for displaying coins/score")]
    public TextMeshProUGUI coinsText;
    
    [Tooltip("TextMeshPro component for game over message")]
    public TextMeshProUGUI gameOverText;
    
    [Tooltip("TextMeshPro component for final score display")]
    public TextMeshProUGUI finalScoreText;
    
    [Tooltip("TextMeshPro component for status notifications (power-ups, etc.)")]
    public TextMeshProUGUI statusText;
    
    [Header("Power-Up UI References")]
    [Tooltip("TextMeshPro component for MilkCup title/description")]
    public TextMeshProUGUI milkCupTitleText;
    
    [Tooltip("TextMeshPro component for ChocoCup title/description")]
    public TextMeshProUGUI chocoCupTitleText;
    
    [Tooltip("TextMeshPro component for Bandage title/description")]
    public TextMeshProUGUI bandageTitleText;
    
    [Header("World Title UI")]
    [Tooltip("One GameObject per world, already positioned in the canvas. Shown with the same animation as power-up titles.")]
    public GameObject[] worldTitleObjects = new GameObject[0];
    
    [Header("UI Button References")]
    [Tooltip("Restart button in game over panel")]
    public Button restartButton;
    
    [Tooltip("Menu button (custom button for game over panel, optional)")]
    public Button menuButton;
    
    [Header("Scene Settings")]
    [Tooltip("Name of the menu scene to load when menu button is clicked (default: 'menu')")]
    public string menuSceneName = "menu";
    
    [Tooltip("Resume button in pause panel")]
    public Button resumeButton;
    
    [Tooltip("Pause button (you can create this button in the UI)")]
    public Button pauseButton;
    
    // Private references to UI elements found at runtime
    private TextMeshProUGUI healthPanelText;
    private TextMeshProUGUI scorePanelText;
    
    // Pause state
    private bool isPaused = false;
    
    // Status text coroutine reference
    private Coroutine statusTextCoroutine;
    
    [Header("Developer Settings")]
    [Tooltip("Key to press to toggle pause menu (default: P)")]
    public KeyCode pauseKey = KeyCode.P;
    
    // Singleton instance for easy access
    private static UIManager instance;
    public static UIManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<UIManager>();
            }
            return instance;
        }
    }
    
    void Awake()
    {
        // Ensure singleton
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeUI();
    }
    
    void InitializeUI()
    {
        // Find Canvas if not assigned
        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("UIManager: No Canvas found in scene. Creating one...");
                CreateCanvas();
            }
        }
        
        // Find panels if not assigned
        FindPanels();
        
        // Find or create TextMeshPro components
        FindOrCreateTextComponents();
        
        // Find or create status text
        FindOrCreateStatusText();
        
        // Setup buttons
        SetupButtons();
        SetupPauseButtons();
        SetupPauseButton();
        
        // Initialize UI state
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        if (gamePausePanel != null)
        {
            gamePausePanel.SetActive(false);
        }
        
        // Initially hide power-up title texts
        if (milkCupTitleText != null)
        {
            milkCupTitleText.gameObject.SetActive(false);
        }
        if (chocoCupTitleText != null)
        {
            chocoCupTitleText.gameObject.SetActive(false);
        }
        if (bandageTitleText != null)
        {
            bandageTitleText.gameObject.SetActive(false);
        }
        if (worldTitleObjects != null)
        {
            for (int i = 0; i < worldTitleObjects.Length; i++)
            {
                if (worldTitleObjects[i] != null)
                    worldTitleObjects[i].SetActive(false);
            }
        }
        
        // Ensure EventSystem exists
        EnsureEventSystem();
        
        // Initialize pause state
        isPaused = false;
        Time.timeScale = 1f;
        
        // Ensure pause button is enabled at start
        if (pauseButton != null)
        {
            pauseButton.interactable = true;
        }
    }
    
    void Update()
    {
        // Developer function: P key to toggle pause
        if (Input.GetKeyDown(pauseKey))
        {
            // Don't allow pausing if game is over
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null && !gameManager.IsGameOver())
            {
                TogglePause();
            }
        }
    }
    
    void CreateCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        RectTransform rectTransform = canvasObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
    }
    
    void FindPanels()
    {
        if (canvas == null) return;
        
        Transform canvasTransform = canvas.transform;
        
        // Find HUD panel
        if (hudPanel == null)
        {
            Transform hudTransform = canvasTransform.Find("HUD");
            if (hudTransform != null)
            {
                hudPanel = hudTransform.gameObject;
            }
        }
        
        // Find Health Panel (could be under HUD or directly under Canvas)
        if (healthPanel == null)
        {
            Transform healthTransform = null;
            if (hudPanel != null)
            {
                healthTransform = hudPanel.transform.Find("Health Panel");
            }
            if (healthTransform == null)
            {
                healthTransform = canvasTransform.Find("Health Panel");
            }
            if (healthTransform != null)
            {
                healthPanel = healthTransform.gameObject;
            }
        }
        
        // Find Score Panel (could be under HUD or directly under Canvas)
        if (scorePanel == null)
        {
            Transform scoreTransform = null;
            if (hudPanel != null)
            {
                scoreTransform = hudPanel.transform.Find("Score Panel");
            }
            if (scoreTransform == null)
            {
                scoreTransform = canvasTransform.Find("Score Panel");
            }
            if (scoreTransform != null)
            {
                scorePanel = scoreTransform.gameObject;
            }
        }
        
        // Find Game Over Panel
        if (gameOverPanel == null)
        {
            Transform gameOverTransform = canvasTransform.Find("Game over Panel");
            if (gameOverTransform == null)
            {
                gameOverTransform = canvasTransform.Find("GameOverPanel");
            }
            if (gameOverTransform != null)
            {
                gameOverPanel = gameOverTransform.gameObject;
                
                // Ensure panel has background image
                Image panelImage = gameOverPanel.GetComponent<Image>();
                if (panelImage == null)
                {
                    panelImage = gameOverPanel.AddComponent<Image>();
                    panelImage.color = new Color(0, 0, 0, 0.8f); // Semi-transparent black background
                    
                    RectTransform panelRect = gameOverPanel.GetComponent<RectTransform>();
                    if (panelRect != null)
                    {
                        panelRect.anchorMin = Vector2.zero;
                        panelRect.anchorMax = Vector2.one;
                        panelRect.sizeDelta = Vector2.zero;
                        panelRect.anchoredPosition = Vector2.zero;
                    }
                }
            }
        }
        
        // Find Game Pause Panel
        if (gamePausePanel == null)
        {
            Transform pauseTransform = canvasTransform.Find("Game pause Panel ");
            if (pauseTransform == null)
            {
                pauseTransform = canvasTransform.Find("Game pause Panel");
            }
            if (pauseTransform != null)
            {
                gamePausePanel = pauseTransform.gameObject;
            }
        }
    }
    
    void FindOrCreateTextComponents()
    {
        // Find or create Lives Text
        if (livesText == null)
        {
            // Try to find in Health Panel
            if (healthPanel != null)
            {
                livesText = healthPanel.GetComponentInChildren<TextMeshProUGUI>();
            }
            
            // If still not found, try to find by name
            if (livesText == null)
            {
                GameObject livesObj = GameObject.Find("LivesText");
                if (livesObj != null)
                {
                    livesText = livesObj.GetComponent<TextMeshProUGUI>();
                }
            }
            
            // Create if still not found
            if (livesText == null)
            {
                GameObject livesObj = new GameObject("LivesText");
                if (healthPanel != null)
                {
                    livesObj.transform.SetParent(healthPanel.transform);
                }
                else if (hudPanel != null)
                {
                    livesObj.transform.SetParent(hudPanel.transform);
                }
                else if (canvas != null)
                {
                    livesObj.transform.SetParent(canvas.transform);
                }
                
                livesText = livesObj.AddComponent<TextMeshProUGUI>();
                livesText.fontSize = 24;
                livesText.color = Color.white;
                livesText.text = "Health: 3";
                livesText.alignment = TextAlignmentOptions.TopLeft;
                
                RectTransform rectTransform = livesObj.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(0, 1);
                rectTransform.pivot = new Vector2(0, 1);
                rectTransform.anchoredPosition = new Vector2(10, -10);
                rectTransform.sizeDelta = new Vector2(200, 30);
            }
        }
        
        // Find or create Coins Text
        if (coinsText == null)
        {
            // Try to find in Score Panel
            if (scorePanel != null)
            {
                coinsText = scorePanel.GetComponentInChildren<TextMeshProUGUI>();
            }
            
            // If still not found, try to find by name
            if (coinsText == null)
            {
                GameObject coinsObj = GameObject.Find("CoinsText");
                if (coinsObj != null)
                {
                    coinsText = coinsObj.GetComponent<TextMeshProUGUI>();
                }
            }
            
            // Create if still not found
            if (coinsText == null)
            {
                GameObject coinsObj = new GameObject("CoinsText");
                if (scorePanel != null)
                {
                    coinsObj.transform.SetParent(scorePanel.transform);
                }
                else if (hudPanel != null)
                {
                    coinsObj.transform.SetParent(hudPanel.transform);
                }
                else if (canvas != null)
                {
                    coinsObj.transform.SetParent(canvas.transform);
                }
                
                coinsText = coinsObj.AddComponent<TextMeshProUGUI>();
                coinsText.fontSize = 24;
                coinsText.color = Color.white;
                coinsText.text = "Coins: 0";
                coinsText.alignment = TextAlignmentOptions.TopLeft;
                
                RectTransform rectTransform = coinsObj.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0, 1);
                rectTransform.anchorMax = new Vector2(0, 1);
                rectTransform.pivot = new Vector2(0, 1);
                rectTransform.anchoredPosition = new Vector2(10, -50);
                rectTransform.sizeDelta = new Vector2(200, 30);
            }
        }
        
        // Find or Create Game Over Text
        if (gameOverPanel != null)
        {
            // Try to find existing Game Over Text
            if (gameOverText == null)
            {
                gameOverText = gameOverPanel.GetComponentInChildren<TextMeshProUGUI>();
                // Try to find by name if not found
                if (gameOverText == null)
                {
                    Transform gameOverTextTransform = gameOverPanel.transform.Find("GameOverText");
                    if (gameOverTextTransform != null)
                    {
                        gameOverText = gameOverTextTransform.GetComponent<TextMeshProUGUI>();
                    }
                }
            }
            
            // Create Game Over Text if not found
            if (gameOverText == null)
            {
                GameObject gameOverObj = new GameObject("GameOverText");
                gameOverObj.transform.SetParent(gameOverPanel.transform);
                gameOverText = gameOverObj.AddComponent<TextMeshProUGUI>();
                gameOverText.fontSize = 48;
                gameOverText.color = Color.white;
                gameOverText.text = "GAME OVER";
                gameOverText.alignment = TextAlignmentOptions.Center;
                
                RectTransform rectTransform = gameOverObj.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = new Vector2(0, 100);
                rectTransform.sizeDelta = new Vector2(400, 60);
            }
            
            // Find or Create Final Score Text
            if (finalScoreText == null)
            {
                // Try to find ScoreText
                Transform scoreTextTransform = gameOverPanel.transform.Find("ScoreText");
                if (scoreTextTransform != null)
                {
                    finalScoreText = scoreTextTransform.GetComponent<TextMeshProUGUI>();
                }
                else
                {
                    // Try to find any text that's not the game over text
                    TextMeshProUGUI[] texts = gameOverPanel.GetComponentsInChildren<TextMeshProUGUI>();
                    foreach (TextMeshProUGUI text in texts)
                    {
                        if (text != gameOverText)
                        {
                            finalScoreText = text;
                            break;
                        }
                    }
                }
            }
            
            // Create Final Score Text if not found
            if (finalScoreText == null)
            {
                GameObject scoreObj = new GameObject("ScoreText");
                scoreObj.transform.SetParent(gameOverPanel.transform);
                finalScoreText = scoreObj.AddComponent<TextMeshProUGUI>();
                finalScoreText.fontSize = 36;
                finalScoreText.color = Color.yellow;
                finalScoreText.text = "Score: 0";
                finalScoreText.alignment = TextAlignmentOptions.Center;
                
                RectTransform rectTransform = scoreObj.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = new Vector2(0, 20);
                rectTransform.sizeDelta = new Vector2(400, 50);
            }
        }
    }
    
    void SetupButtons()
    {
        // Find or Create Restart Button
        if (gameOverPanel != null)
        {
            // Try to find existing button
            if (restartButton == null)
            {
                restartButton = gameOverPanel.GetComponentInChildren<Button>();
                if (restartButton == null)
                {
                    Transform buttonTransform = gameOverPanel.transform.Find("RestartButton");
                    if (buttonTransform != null)
                    {
                        restartButton = buttonTransform.GetComponent<Button>();
                    }
                }
            }
            
            // Create Restart Button if not found
            if (restartButton == null)
            {
                GameObject buttonObj = new GameObject("RestartButton");
                buttonObj.transform.SetParent(gameOverPanel.transform);
                
                // Add Image component for button background
                Image buttonImage = buttonObj.AddComponent<Image>();
                buttonImage.color = new Color(0.2f, 0.6f, 0.2f); // Green color
                
                restartButton = buttonObj.AddComponent<Button>();
                
                // Button text
                GameObject buttonTextObj = new GameObject("Text");
                buttonTextObj.transform.SetParent(buttonObj.transform);
                TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
                buttonText.fontSize = 32;
                buttonText.color = Color.white;
                buttonText.text = "TRY AGAIN";
                buttonText.alignment = TextAlignmentOptions.Center;
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
            }
            
            // Setup restart button listener
            if (restartButton != null)
            {
                // Remove existing listeners to avoid duplicates
                restartButton.onClick.RemoveAllListeners();
                
                // Add listener to restart the scene
                restartButton.onClick.AddListener(() =>
                {
                    // Resume time scale in case game was paused
                    Time.timeScale = 1f;
                    
                    // Reload the current scene
                    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                });
            }
            
            // Setup menu button listener if assigned
            if (menuButton != null)
            {
                // Remove existing listeners to avoid duplicates
                menuButton.onClick.RemoveAllListeners();
                
                // Add listener to load menu scene
                menuButton.onClick.AddListener(() =>
                {
                    LoadMenuScene();
                });
            }
        }
    }
    
    void SetupPauseButtons()
    {
        // Find or Create Resume Button
        if (gamePausePanel != null)
        {
            // Try to find existing button
            if (resumeButton == null)
            {
                resumeButton = gamePausePanel.GetComponentInChildren<Button>();
                if (resumeButton == null)
                {
                    Transform buttonTransform = gamePausePanel.transform.Find("ResumeButton");
                    if (buttonTransform != null)
                    {
                        resumeButton = buttonTransform.GetComponent<Button>();
                    }
                }
            }
            
            // Create Resume Button if not found
            if (resumeButton == null)
            {
                GameObject buttonObj = new GameObject("ResumeButton");
                buttonObj.transform.SetParent(gamePausePanel.transform);
                
                // Add Image component for button background
                Image buttonImage = buttonObj.AddComponent<Image>();
                buttonImage.color = new Color(0.2f, 0.6f, 0.2f); // Green color
                
                resumeButton = buttonObj.AddComponent<Button>();
                
                // Button text
                GameObject buttonTextObj = new GameObject("Text");
                buttonTextObj.transform.SetParent(buttonObj.transform);
                TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
                buttonText.fontSize = 32;
                buttonText.color = Color.white;
                buttonText.text = "RESUME";
                buttonText.alignment = TextAlignmentOptions.Center;
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
            }
            
            // Setup resume button listener
            if (resumeButton != null)
            {
                // Remove existing listeners to avoid duplicates
                resumeButton.onClick.RemoveAllListeners();
                
                // Add listener to hide pause (resume game)
                resumeButton.onClick.AddListener(() =>
                {
                    HidePause();
                });
            }
        }
    }
    
    void SetupPauseButton()
    {
        // Setup pause button listener if it exists
        if (pauseButton != null)
        {
            // Remove existing listeners to avoid duplicates
            pauseButton.onClick.RemoveAllListeners();
            
            // Add listener to show pause
            pauseButton.onClick.AddListener(() =>
            {
                // Don't allow pausing if game is over
                GameManager gameManager = FindObjectOfType<GameManager>();
                if (gameManager != null && !gameManager.IsGameOver())
                {
                    ShowPause();
                }
            });
        }
    }
    
    void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
        }
    }
    
    // Public methods for updating UI
    
    /// <summary>
    /// Update the lives/health display
    /// </summary>
    public void UpdateLives(int lives)
    {
        if (livesText != null)
        {
            livesText.text = "Health: " + lives.ToString();
        }
        
        // Also update health panel text if it exists separately
        if (healthPanelText == null && healthPanel != null)
        {
            healthPanelText = healthPanel.GetComponentInChildren<TextMeshProUGUI>();
        }
        if (healthPanelText != null && healthPanelText != livesText)
        {
            healthPanelText.text = "Health: " + lives.ToString();
        }
    }
    
    /// <summary>
    /// Update the coins/score display
    /// </summary>
    public void UpdateCoins(int coins)
    {
        if (coinsText != null)
        {
            coinsText.text = "Score: " + coins.ToString();
        }
        
        // Also update score panel text if it exists separately
        if (scorePanelText == null && scorePanel != null)
        {
            scorePanelText = scorePanel.GetComponentInChildren<TextMeshProUGUI>();
        }
        if (scorePanelText != null && scorePanelText != coinsText)
        {
            scorePanelText.text = "Score: " + coins.ToString();
        }
    }
    
    /// <summary>
    /// Update both lives and coins
    /// </summary>
    public void UpdateUI(int lives, int coins)
    {
        UpdateLives(lives);
        UpdateCoins(coins);
    }
    
    /// <summary>
    /// Show the game over panel with final score
    /// </summary>
    public void ShowGameOver(int finalScore)
    {
        // Ensure game over panel exists
        if (gameOverPanel == null)
        {
            FindPanels();
        }
        
        // If still no panel, create it
        if (gameOverPanel == null && canvas != null)
        {
            GameObject panelObj = new GameObject("Game over Panel");
            panelObj.transform.SetParent(canvas.transform);
            gameOverPanel = panelObj;
            
            // Add Image component for background
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f); // Semi-transparent black background
            
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;
        }
        
        if (gameOverPanel != null)
        {
            // Ensure TextMeshPro components exist
            if (gameOverText == null || finalScoreText == null)
            {
                FindOrCreateTextComponents();
            }
            
            // Ensure button exists
            if (restartButton == null)
            {
                SetupButtons();
            }
            
            // Update final score text
            if (finalScoreText != null)
            {
                finalScoreText.text = "Score: " + finalScore.ToString();
            }
            else
            {
                Debug.LogWarning("UIManager: Final Score Text not found! Attempting to create...");
                FindOrCreateTextComponents();
                if (finalScoreText != null)
                {
                    finalScoreText.text = "Score: " + finalScore.ToString();
                }
            }
            
            // Ensure all child elements are active and visible
            if (gameOverText != null)
            {
                gameOverText.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("UIManager: Game Over Text is null!");
            }
            
            if (finalScoreText != null)
            {
                finalScoreText.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("UIManager: Final Score Text is null!");
            }
            
            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("UIManager: Restart Button is null!");
            }
            
            // Show menu button if assigned
            if (menuButton != null)
            {
                menuButton.gameObject.SetActive(true);
            }
            
            // Ensure panel RectTransform is properly set up
            RectTransform panelRect = gameOverPanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.sizeDelta = Vector2.zero;
                panelRect.anchoredPosition = Vector2.zero;
            }
            
            // Disable HUD when game over appears
            if (hudPanel == null)
            {
                FindPanels();
            }
            if (hudPanel != null)
            {
                hudPanel.SetActive(false);
            }
            
            // Show the panel
            gameOverPanel.SetActive(true);
            
            // Disable pause button when game is over
            if (pauseButton != null)
            {
                pauseButton.interactable = false;
            }
            
            Debug.Log("UIManager: Game Over Panel shown. Text: " + (gameOverText != null) + ", Score: " + (finalScoreText != null) + ", Button: " + (restartButton != null));
        }
        else
        {
            Debug.LogError("UIManager: Could not find or create Game Over Panel!");
        }
    }
    
    /// <summary>
    /// Hide the game over panel
    /// </summary>
    public void HideGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        // Hide menu button if assigned
        if (menuButton != null)
        {
            menuButton.gameObject.SetActive(false);
        }
        
        // Re-enable HUD when game over is hidden (e.g., on restart)
        if (hudPanel != null)
        {
            hudPanel.SetActive(true);
        }
        
        // Re-enable pause button when game restarts
        if (pauseButton != null)
        {
            pauseButton.interactable = true;
        }
    }
    
    /// <summary>
    /// Show the pause panel and pause the game
    /// </summary>
    public void ShowPause()
    {
        if (isPaused) return; // Already paused
        
        isPaused = true;
        Time.timeScale = 0f; // Pause game time
        
        // Ensure pause panel exists
        if (gamePausePanel == null)
        {
            FindPanels();
        }
        
        // Create pause panel if it doesn't exist
        if (gamePausePanel == null && canvas != null)
        {
            GameObject panelObj = new GameObject("Game pause Panel");
            panelObj.transform.SetParent(canvas.transform);
            gamePausePanel = panelObj;
            
            // Add Image component for background
            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f); // Semi-transparent black background
            
            RectTransform panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;
            
            // Create "PAUSED" text
            GameObject pausedTextObj = new GameObject("PausedText");
            pausedTextObj.transform.SetParent(panelObj.transform);
            TextMeshProUGUI pausedText = pausedTextObj.AddComponent<TextMeshProUGUI>();
            pausedText.fontSize = 48;
            pausedText.color = Color.white;
            pausedText.text = "PAUSED";
            pausedText.alignment = TextAlignmentOptions.Center;
            
            RectTransform pausedRect = pausedTextObj.GetComponent<RectTransform>();
            pausedRect.anchorMin = new Vector2(0.5f, 0.5f);
            pausedRect.anchorMax = new Vector2(0.5f, 0.5f);
            pausedRect.pivot = new Vector2(0.5f, 0.5f);
            pausedRect.anchoredPosition = new Vector2(0, 100);
            pausedRect.sizeDelta = new Vector2(400, 60);
        }
        
        // Ensure resume button exists
        if (gamePausePanel != null)
        {
            if (resumeButton == null)
            {
                SetupPauseButtons();
            }
            
            // Ensure all child elements are active
            if (resumeButton != null)
            {
                resumeButton.gameObject.SetActive(true);
            }
            
            // Ensure panel RectTransform is properly set up
            RectTransform panelRect = gamePausePanel.GetComponent<RectTransform>();
            if (panelRect != null)
            {
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.sizeDelta = Vector2.zero;
                panelRect.anchoredPosition = Vector2.zero;
            }
            
            gamePausePanel.SetActive(true);
            Debug.Log("UIManager: Game Paused - Panel: " + (gamePausePanel != null) + ", Button: " + (resumeButton != null));
        }
        else
        {
            Debug.LogError("UIManager: Could not find or create Pause Panel!");
        }
        
        // Disable pause button when game is paused
        if (pauseButton != null)
        {
            pauseButton.interactable = false;
        }
    }
    
    /// <summary>
    /// Hide the pause panel and unpause the game
    /// </summary>
    public void HidePause()
    {
        if (!isPaused) return; // Already unpaused
        
        isPaused = false;
        Time.timeScale = 1f; // Resume game time
        
        if (gamePausePanel != null)
        {
            gamePausePanel.SetActive(false);
            Debug.Log("UIManager: Game Resumed");
        }
        
        // Re-enable pause button when game is resumed
        if (pauseButton != null)
        {
            pauseButton.interactable = true;
        }
    }
    
    /// <summary>
    /// Toggle pause panel visibility and game pause state
    /// </summary>
    public void TogglePause()
    {
        if (isPaused)
        {
            HidePause();
        }
        else
        {
            ShowPause();
        }
    }
    
    /// <summary>
    /// Check if game is currently paused
    /// </summary>
    public bool IsPaused()
    {
        return isPaused;
    }
    
    /// <summary>
    /// Handle application pause (when user switches apps or OS pauses the app)
    /// </summary>
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            // App is being paused by the OS (e.g., user switched apps)
            // Only pause if game is not already over
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null && !gameManager.IsGameOver() && !isPaused)
            {
                ShowPause();
            }
        }
        // Note: We don't auto-resume when the app comes back to focus
        // The user can manually resume using the resume button
    }
    
    /// <summary>
    /// Handle application focus (when app loses/gains focus)
    /// </summary>
    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            // App lost focus (e.g., user switched apps, notification appeared)
            // Only pause if game is not already over
            GameManager gameManager = FindObjectOfType<GameManager>();
            if (gameManager != null && !gameManager.IsGameOver() && !isPaused)
            {
                ShowPause();
            }
        }
        // Note: We don't auto-resume when the app regains focus
        // The user can manually resume using the resume button
    }
    
    /// <summary>
    /// Find or create the status text component for power-up notifications
    /// </summary>
    void FindOrCreateStatusText()
    {
        if (statusText == null)
        {
            // Try to find by name
            GameObject statusObj = GameObject.Find("StatusText");
            if (statusObj != null)
            {
                statusText = statusObj.GetComponent<TextMeshProUGUI>();
            }
            
            // Create if still not found
            if (statusText == null)
            {
                GameObject statusObjNew = new GameObject("StatusText");
                if (canvas != null)
                {
                    statusObjNew.transform.SetParent(canvas.transform);
                }
                else if (hudPanel != null)
                {
                    statusObjNew.transform.SetParent(hudPanel.transform);
                }
                
                statusText = statusObjNew.AddComponent<TextMeshProUGUI>();
                statusText.fontSize = 36;
                statusText.color = Color.yellow;
                statusText.text = "";
                statusText.alignment = TextAlignmentOptions.Center;
                statusText.fontStyle = FontStyles.Bold;
                
                RectTransform rectTransform = statusObjNew.GetComponent<RectTransform>();
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = new Vector2(0, 100);
                rectTransform.sizeDelta = new Vector2(400, 50);
            }
        }
        
        // Initially hide the status text
        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Show a status message (e.g., power-up notifications) for 1 second
    /// </summary>
    public void ShowStatusText(string message)
    {
        if (statusText == null)
        {
            FindOrCreateStatusText();
        }
        
        if (statusText != null)
        {
            // Stop any existing coroutine
            if (statusTextCoroutine != null)
            {
                StopCoroutine(statusTextCoroutine);
            }
            
            // Start new coroutine to show and hide the text
            statusTextCoroutine = StartCoroutine(ShowStatusTextCoroutine(message));
        }
    }
    
    /// <summary>
    /// Coroutine to show status text for 1 second
    /// </summary>
    private System.Collections.IEnumerator ShowStatusTextCoroutine(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.gameObject.SetActive(true);
            
            yield return new WaitForSeconds(1f);
            
            statusText.gameObject.SetActive(false);
            statusTextCoroutine = null;
        }
    }
    
    /// <summary>
    /// Show the appropriate power-up title text when collected
    /// </summary>
    public void ShowPowerUpTitle(PowerUpItem.ItemType itemType, float duration = 2f)
    {
        TextMeshProUGUI titleText = null;
        
        switch (itemType)
        {
            case PowerUpItem.ItemType.MilkCup:
                titleText = milkCupTitleText;
                break;
            case PowerUpItem.ItemType.ChocoCup:
                titleText = chocoCupTitleText;
                break;
            case PowerUpItem.ItemType.Bandage:
                titleText = bandageTitleText;
                break;
        }
        
        if (titleText != null)
        {
            // Hide all other title texts first
            if (milkCupTitleText != null && milkCupTitleText != titleText)
            {
                milkCupTitleText.gameObject.SetActive(false);
            }
            if (chocoCupTitleText != null && chocoCupTitleText != titleText)
            {
                chocoCupTitleText.gameObject.SetActive(false);
            }
            if (bandageTitleText != null && bandageTitleText != titleText)
            {
                bandageTitleText.gameObject.SetActive(false);
            }
            
            // Show the selected title text
            titleText.gameObject.SetActive(true);
            
            // Start animation coroutine
            if (statusTextCoroutine != null)
            {
                StopCoroutine(statusTextCoroutine);
            }
            statusTextCoroutine = StartCoroutine(AnimatePowerUpTitleCoroutine(titleText, duration));
        }
    }
    
    /// <summary>
    /// Coroutine to animate and hide power-up title
    /// </summary>
    private System.Collections.IEnumerator AnimatePowerUpTitleCoroutine(TextMeshProUGUI titleText, float duration)
    {
        if (titleText == null)
        {
            yield break;
        }
        
        RectTransform rectTransform = titleText.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            yield break;
        }
        
        // Store original scale
        Vector3 originalScale = rectTransform.localScale;
        Vector3 currentScale = originalScale;
        
        // Start at scale 0 in Y
        currentScale.y = 0f;
        rectTransform.localScale = currentScale;
        
        // Step 1: Scale to 1.2 in Y over 0.25 seconds
        float elapsed = 0f;
        float targetY = 1.2f;
        while (elapsed < 0.25f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / 0.25f;
            currentScale.y = Mathf.Lerp(0f, targetY, progress);
            rectTransform.localScale = currentScale;
            yield return null;
        }
        currentScale.y = targetY;
        rectTransform.localScale = currentScale;
        
        // Step 2: Scale to 0.9 in Y over 0.1 seconds
        elapsed = 0f;
        targetY = 0.9f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / 0.1f;
            currentScale.y = Mathf.Lerp(1.2f, targetY, progress);
            rectTransform.localScale = currentScale;
            yield return null;
        }
        currentScale.y = targetY;
        rectTransform.localScale = currentScale;
        
        // Step 3: Scale to 1.0 in Y over 0.05 seconds
        elapsed = 0f;
        targetY = 1.0f;
        while (elapsed < 0.05f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / 0.05f;
            currentScale.y = Mathf.Lerp(0.9f, targetY, progress);
            rectTransform.localScale = currentScale;
            yield return null;
        }
        currentScale.y = targetY;
        rectTransform.localScale = currentScale;
        
        // Wait for the remaining duration (total duration minus animation time which is 0.4 seconds)
        float remainingTime = duration - 0.4f;
        if (remainingTime > 0f)
        {
            yield return new WaitForSeconds(remainingTime);
        }
        
        // Hide the title text
        if (titleText != null)
        {
            titleText.gameObject.SetActive(false);
            // Reset scale to original
            rectTransform.localScale = originalScale;
        }
        statusTextCoroutine = null;
    }
    
    /// <summary>
    /// Show the world title GameObject at the given index (one per world), with same animation as power-up titles.
    /// </summary>
    public void ShowWorldTitle(int worldIndex, float duration = 2f)
    {
        if (worldTitleObjects == null || worldIndex < 0 || worldIndex >= worldTitleObjects.Length)
            return;
        GameObject titleObj = worldTitleObjects[worldIndex];
        if (titleObj == null)
            return;
        // Hide all world title objects first
        for (int i = 0; i < worldTitleObjects.Length; i++)
        {
            if (worldTitleObjects[i] != null)
                worldTitleObjects[i].SetActive(false);
        }
        titleObj.SetActive(true);
        if (statusTextCoroutine != null)
            StopCoroutine(statusTextCoroutine);
        statusTextCoroutine = StartCoroutine(AnimateTitleObjectCoroutine(titleObj, duration));
    }

    /// <summary>
    /// Same scale-in animation as power-up titles, but for any GameObject with a RectTransform.
    /// </summary>
    private System.Collections.IEnumerator AnimateTitleObjectCoroutine(GameObject titleObject, float duration)
    {
        if (titleObject == null)
        {
            statusTextCoroutine = null;
            yield break;
        }
        RectTransform rectTransform = titleObject.GetComponent<RectTransform>();
        if (rectTransform == null)
            rectTransform = titleObject.GetComponentInChildren<RectTransform>();
        if (rectTransform == null)
        {
            titleObject.SetActive(false);
            statusTextCoroutine = null;
            yield break;
        }
        Vector3 originalScale = rectTransform.localScale;
        Vector3 currentScale = originalScale;
        currentScale.y = 0f;
        rectTransform.localScale = currentScale;
        // Step 1: Scale to 1.2 in Y over 0.25 seconds
        float elapsed = 0f;
        while (elapsed < 0.25f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / 0.25f;
            currentScale.y = Mathf.Lerp(0f, 1.2f, progress);
            rectTransform.localScale = currentScale;
            yield return null;
        }
        currentScale.y = 1.2f;
        rectTransform.localScale = currentScale;
        // Step 2: Scale to 0.9 in Y over 0.1 seconds
        elapsed = 0f;
        while (elapsed < 0.1f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / 0.1f;
            currentScale.y = Mathf.Lerp(1.2f, 0.9f, progress);
            rectTransform.localScale = currentScale;
            yield return null;
        }
        currentScale.y = 0.9f;
        rectTransform.localScale = currentScale;
        // Step 3: Scale to 1.0 in Y over 0.05 seconds
        elapsed = 0f;
        while (elapsed < 0.05f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / 0.05f;
            currentScale.y = Mathf.Lerp(0.9f, 1f, progress);
            rectTransform.localScale = currentScale;
            yield return null;
        }
        currentScale.y = 1f;
        rectTransform.localScale = currentScale;
        float remainingTime = duration - 0.4f;
        if (remainingTime > 0f)
            yield return new WaitForSeconds(remainingTime);
        if (titleObject != null)
        {
            titleObject.SetActive(false);
            rectTransform.localScale = originalScale;
        }
        statusTextCoroutine = null;
    }
    
    /// <summary>
    /// Loads the menu scene when menu button is clicked
    /// </summary>
    public void LoadMenuScene()
    {
        if (string.IsNullOrEmpty(menuSceneName))
        {
            Debug.LogWarning("UIManager: Menu scene name is not set! Cannot load menu scene.");
            return;
        }
        
        // Resume time scale in case game was paused
        Time.timeScale = 1f;
        
        // Load the menu scene
        SceneManager.LoadScene(menuSceneName);
    }
}
