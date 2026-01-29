using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Settings")]
    public EnemyPosition position = EnemyPosition.Down;
    public int laneWidth = 1; // 1 or 2 lanes
    
    public enum EnemyPosition
    {
        Up,
        Down,
        Both
    }
    
    private float forwardSpeed = 10f;
    private GameManager gameManager;
    
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            forwardSpeed = gameManager.GetForwardSpeed();
        }
    }
    
    void OnEnable()
    {
        SetupEnemy();
    }
    
    void Update()
    {
        // Don't move if game is over
        if (gameManager != null && gameManager.IsGameOver())
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
    
    public void SetupEnemy()
    {
        // Set color to red
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // Create new material instance to avoid affecting other objects
            Material mat = new Material(renderer.material);
            mat.color = Color.red;
            renderer.material = mat;
        }
        
        // Position based on enemy type
        float yPos = 0f;
        float scaleY = 1f;
        
        switch (position)
        {
            case EnemyPosition.Up:
                yPos = 1.5f; // Above ground
                scaleY = 1f;
                break;
            case EnemyPosition.Down:
                yPos = 0f; // On ground
                scaleY = 1f;
                break;
            case EnemyPosition.Both:
                yPos = 0.75f; // Middle height
                scaleY = 2f; // Taller to cover both
                break;
        }
        
        Vector3 currentPos = transform.position;
        transform.position = new Vector3(currentPos.x, yPos, currentPos.z);
        
        Vector3 currentScale = transform.localScale;
        transform.localScale = new Vector3(currentScale.x * laneWidth, scaleY, currentScale.z);
        
        // Add collider if not present
        Collider existingCollider = GetComponent<Collider>();
        if (existingCollider == null)
        {
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
        }
        else
        {
            // Ensure existing collider is a trigger
            existingCollider.isTrigger = true;
        }
        
        // Add Rigidbody for trigger detection (kinematic so it doesn't fall)
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        // Set tag
        gameObject.tag = "Enemy";
    }
    
    public void SetLane(int lane)
    {
        float laneWidth = 2f;
        float xPos = (lane - 1) * laneWidth;
        transform.position = new Vector3(xPos, transform.position.y, transform.position.z);
    }
    
    public void SetLaneWidth(int width)
    {
        laneWidth = Mathf.Clamp(width, 1, 2);
    }
}
