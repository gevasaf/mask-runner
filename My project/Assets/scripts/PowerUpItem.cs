using UnityEngine;

public class PowerUpItem : MonoBehaviour
{
    public enum ItemType
    {
        MilkCup,    // Adds 10 coins
        ChocoCup,   // Speed boost + invincibility for 5 seconds
        Bandage     // Adds +1 life
    }
    
    public enum PowerUpPosition
    {
        Up,
        Down
    }
    
    [Header("Power-Up Settings")]
    public ItemType itemType = ItemType.MilkCup;
    public PowerUpPosition position = PowerUpPosition.Down;
    
    [Header("ChocoCup Settings")]
    [Tooltip("Speed multiplier for ChocoCup (default: 2.0)")]
    public float speedMultiplier = 2f;
    
    [Tooltip("Duration of speed boost in seconds (default: 5.0)")]
    public float speedBoostDuration = 5f;
    
    private GameManager gameManager;
    private UIManager uiManager;
    private float forwardSpeed = 10f;
    
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        uiManager = UIManager.Instance;
        
        if (gameManager != null)
        {
            forwardSpeed = gameManager.GetForwardSpeed();
        }
        
        SetupPowerUp();
    }
    
    void Update()
    {
        // Don't move if game is over
        if (gameManager == null || gameManager.IsGameOver())
        {
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
    
    public void SetupPowerUp()
    {
        // Position based on power-up position type
        float yPos = 0f;
        
        switch (position)
        {
            case PowerUpPosition.Up:
                yPos = 1.5f; // Above ground (need to jump)
                break;
            case PowerUpPosition.Down:
                yPos = 0f; // On ground (don't jump)
                break;
        }
        
        Vector3 currentPos = transform.position;
        transform.position = new Vector3(currentPos.x, yPos, currentPos.z);
        
        // Ensure this object has a collider set as trigger
        Collider collider = GetComponent<Collider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<SphereCollider>();
        }
        collider.isTrigger = true;
        
        // Add Rigidbody for trigger detection (kinematic so it doesn't fall)
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // Check if player can collect based on position
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                bool canCollect = false;
                
                // Player is on ground (not jumping), can collect Down power-ups
                if (!player.IsJumping() && position == PowerUpPosition.Down)
                {
                    canCollect = true;
                }
                
                // Player is jumping, can collect Up power-ups
                if (player.IsJumping() && position == PowerUpPosition.Up)
                {
                    canCollect = true;
                }
                
                if (!canCollect)
                {
                    return; // Can't collect this power-up from current position
                }
            }
            
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }
            
            if (uiManager == null)
            {
                uiManager = UIManager.Instance;
            }
            
            // Apply power-up effect based on type
            switch (itemType)
            {
                case ItemType.MilkCup:
                    if (gameManager != null)
                    {
                        gameManager.AddCoins(10);
                    }
                    if (uiManager != null)
                    {
                        uiManager.ShowPowerUpTitle(ItemType.MilkCup);
                    }
                    break;
                    
                case ItemType.ChocoCup:
                    if (gameManager != null)
                    {
                        gameManager.ActivateSpeedBoost(speedMultiplier, speedBoostDuration);
                    }
                    if (uiManager != null)
                    {
                        uiManager.ShowPowerUpTitle(ItemType.ChocoCup);
                    }
                    break;
                    
                case ItemType.Bandage:
                    if (gameManager != null)
                    {
                        gameManager.AddLives(1);
                    }
                    if (uiManager != null)
                    {
                        uiManager.ShowPowerUpTitle(ItemType.Bandage);
                    }
                    break;
            }
            
            // Destroy the power-up item
            Destroy(gameObject);
        }
    }
}
