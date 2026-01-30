using UnityEngine;

public class InfiniteRunner : MonoBehaviour
{
    public float forwardSpeed = 10f;
    private GameManager gameManager;
    
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            forwardSpeed = gameManager.GetForwardSpeed();
        }
    }
    
    void Update()
    {
        // Move this object backward to create infinite runner effect
        // This should be attached to obstacles, coins, and environment objects
        transform.Translate(Vector3.back * forwardSpeed * Time.deltaTime);
        
        // Destroy when off screen
        if (transform.position.z < -20f)
        {
            Destroy(gameObject);
        }
    }
}
