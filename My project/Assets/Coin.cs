using UnityEngine;

public class Coin : MonoBehaviour
{
    private float forwardSpeed = 10f;
    private GameManager gameManager;
    private float rotationSpeed = 180f;
    private static Material yellowMaterial; // Shared material for all coins
    
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            forwardSpeed = gameManager.GetForwardSpeed();
        }
        
        // Make coin yellow/gold - use shared material to avoid allocations
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            // Create shared material only once
            if (yellowMaterial == null)
            {
                yellowMaterial = new Material(Shader.Find("Standard"));
                yellowMaterial.color = Color.yellow;
            }
            renderer.material = yellowMaterial;
        }
        
        // Add collider if not present
        Collider existingCollider = GetComponent<Collider>();
        if (existingCollider == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 0.5f;
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
        gameObject.tag = "Coin";
    }
    
    void Update()
    {
        // Don't move if game is over - cache check to avoid repeated null checks
        if (gameManager == null || gameManager.IsGameOver())
        {
            return;
        }
        
        // Move backward (toward player)
        transform.Translate(Vector3.back * forwardSpeed * Time.deltaTime, Space.World);
        
        // Rotate coin
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);
        
        // Destroy when well past the player (player is typically at z=0)
        // Give some buffer to ensure coins can be collected
        if (transform.position.z < -15f)
        {
            Destroy(gameObject);
        }
    }
    
    public void SetLane(int lane)
    {
        float laneWidth = 2f;
        float xPos = (lane - 1) * laneWidth;
        transform.position = new Vector3(xPos, transform.position.y, transform.position.z);
    }
}
