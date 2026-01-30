using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    public int laneWidth = 1; // 1 or 2 lanes
    public bool isJumpable = false; // Can player jump over this enemy?
    public bool isDuckable = false; // Can player duck/slide under this enemy?
    
    private float forwardSpeed = 10f;
    private GameManager gameManager;
    private bool isGameOver = false;
    
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            forwardSpeed = gameManager.GetForwardSpeed();
            isGameOver = gameManager.IsGameOver();
        }
    }
    
    void Update()
    {
        // Early exit if game is over
        if (isGameOver)
        {
            return;
        }
        
        // Update game over state periodically (optimized check)
        if (gameManager != null && gameManager.IsGameOver())
        {
            isGameOver = true;
            return;
        }
        
        // Move backward (toward player)
        transform.Translate(Vector3.back * forwardSpeed * Time.deltaTime);
        
        // Destroy when off screen
        if (transform.position.z < -10f)
        {
            Destroy(gameObject);
        }
    }
}
