using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Lane Settings")]
    public float laneWidth = 2f;
    public float laneSwitchSpeed = 10f;
    
    [Header("Movement")]
    public float jumpHeight = 2f;
    public float jumpDuration = 0.5f;
    public float slideHeight = 0.5f;
    public float slideDuration = 0.5f;
    
    private SwipeDetector swipeDetector;
    private int currentLane = 1; // 0 = left, 1 = middle, 2 = right
    private float targetX;
    private bool isJumping = false;
    private bool isSliding = false;
    private float originalY;
    private float originalScaleY;
    private Vector3 originalScale;
    
    private GameManager gameManager;
    private bool gameOver = false;
    
    // Track recently hit enemies to prevent multiple hits from the same enemy
    private HashSet<GameObject> recentlyHitEnemies = new HashSet<GameObject>();
    private float hitCooldown = 0.5f; // Cooldown time in seconds
    
    void Start()
    {
        swipeDetector = FindObjectOfType<SwipeDetector>();
        if (swipeDetector == null)
        {
            GameObject swipeObj = new GameObject("SwipeDetector");
            swipeDetector = swipeObj.AddComponent<SwipeDetector>();
        }
        
        gameManager = FindObjectOfType<GameManager>();
        
        originalY = transform.position.y;
        originalScale = transform.localScale;
        originalScaleY = originalScale.y;
        targetX = GetLaneX(currentLane);
        
        // Ensure player has collider and tag
        if (GetComponent<Collider>() == null)
        {
            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
        }
        
        // Add Rigidbody for trigger detection (kinematic so it doesn't fall)
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true; // Don't use physics, just for trigger detection
            rb.useGravity = false;
        }
        
        if (!gameObject.CompareTag("Player"))
        {
            gameObject.tag = "Player";
        }
    }
    
    void Update()
    {
        HandleInput();
        UpdateMovement();
    }
    
    void HandleInput()
    {
        // Don't handle input if game is over
        if (gameOver)
            return;
            
        if (swipeDetector.HasSwipe())
        {
            SwipeDetector.SwipeDirection swipe = swipeDetector.GetSwipe();
            
            switch (swipe)
            {
                case SwipeDetector.SwipeDirection.Up:
                    if (!isJumping && !isSliding)
                        StartCoroutine(Jump());
                    break;
                    
                case SwipeDetector.SwipeDirection.Down:
                    if (!isJumping && !isSliding)
                        StartCoroutine(Slide());
                    break;
                    
                case SwipeDetector.SwipeDirection.Left:
                    if (!isJumping && !isSliding)
                        ChangeLane(-1);
                    break;
                    
                case SwipeDetector.SwipeDirection.Right:
                    if (!isJumping && !isSliding)
                        ChangeLane(1);
                    break;
            }
        }
        
#if UNITY_EDITOR
        // Keyboard controls for development (only in Unity Editor)
        HandleKeyboardInput();
#endif
    }
    
#if UNITY_EDITOR
    void HandleKeyboardInput()
    {
        // Jump: Up Arrow, W, or Space
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
        {
            if (!isJumping && !isSliding)
                StartCoroutine(Jump());
        }
        
        // Slide: Down Arrow or S
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            if (!isJumping && !isSliding)
                StartCoroutine(Slide());
        }
        
        // Left Lane: Left Arrow or A
        if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            if (!isJumping && !isSliding)
                ChangeLane(-1);
        }
        
        // Right Lane: Right Arrow or D
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            if (!isJumping && !isSliding)
                ChangeLane(1);
        }
    }
#endif
    
    void UpdateMovement()
    {
        // Move forward (actually the world moves backward in infinite runner)
        // Lane switching
        float currentX = transform.position.x;
        float newX = Mathf.Lerp(currentX, targetX, laneSwitchSpeed * Time.deltaTime);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }
    
    void ChangeLane(int direction)
    {
        int newLane = currentLane + direction;
        if (newLane >= 0 && newLane <= 2)
        {
            currentLane = newLane;
            targetX = GetLaneX(currentLane);
        }
    }
    
    float GetLaneX(int lane)
    {
        return (lane - 1) * laneWidth; // -laneWidth, 0, laneWidth
    }
    
    IEnumerator Jump()
    {
        isJumping = true;
        float elapsed = 0f;
        float startY = transform.position.y;
        
        // Jump up
        while (elapsed < jumpDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (jumpDuration / 2f);
            float y = Mathf.Lerp(startY, startY + jumpHeight, progress);
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
            yield return null;
        }
        
        // Fall down
        elapsed = 0f;
        while (elapsed < jumpDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (jumpDuration / 2f);
            float y = Mathf.Lerp(startY + jumpHeight, originalY, progress);
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
            yield return null;
        }
        
        transform.position = new Vector3(transform.position.x, originalY, transform.position.z);
        isJumping = false;
    }
    
    IEnumerator Slide()
    {
        isSliding = true;
        float elapsed = 0f;
        float startY = transform.position.y;
        float startScaleY = transform.localScale.y;
        
        // Slide down
        while (elapsed < slideDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (slideDuration / 2f);
            float y = Mathf.Lerp(startY, originalY - slideHeight, progress);
            float scaleY = Mathf.Lerp(startScaleY, originalScaleY * 0.5f, progress);
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
            transform.localScale = new Vector3(originalScale.x, scaleY, originalScale.z);
            yield return null;
        }
        
        // Slide up
        elapsed = 0f;
        while (elapsed < slideDuration / 2f)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / (slideDuration / 2f);
            float y = Mathf.Lerp(originalY - slideHeight, originalY, progress);
            float scaleY = Mathf.Lerp(originalScaleY * 0.5f, originalScaleY, progress);
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
            transform.localScale = new Vector3(originalScale.x, scaleY, originalScale.z);
            yield return null;
        }
        
        transform.position = new Vector3(transform.position.x, originalY, transform.position.z);
        transform.localScale = originalScale;
        isSliding = false;
    }
    
    public int GetCurrentLane()
    {
        return currentLane;
    }
    
    public bool IsJumping()
    {
        return isJumping;
    }
    
    public bool IsSliding()
    {
        return isSliding;
    }
    
    public void SetGameOver(bool isOver)
    {
        gameOver = isOver;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            // Find the root enemy GameObject (the one with the Enemy component)
            // This handles cases where colliders are on child objects
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy == null)
            {
                // If Enemy component not on this object, search in parent
                enemy = other.GetComponentInParent<Enemy>();
            }
            
            if (enemy == null)
            {
                return; // No enemy component found
            }
            
            // Use the root enemy GameObject for tracking, not the collider's GameObject
            GameObject rootEnemyObject = enemy.gameObject;
            
            // Prevent multiple hits from the same enemy
            if (recentlyHitEnemies.Contains(rootEnemyObject))
            {
                return; // Already hit this enemy recently, ignore
            }
            
            // Check if enemy can be avoided based on player's current state
            bool canCollide = true; // Default to collision unless player can avoid it
            
            // Player is on ground (not jumping, not sliding)
            if (!isJumping && !isSliding)
            {
                // Can avoid collision if enemy is duckable (can slide under it)
                if (enemy.isDuckable)
                {
                    canCollide = false;
                }
            }
            // Player is jumping
            else if (isJumping)
            {
                // Can avoid collision if enemy is jumpable (can jump over it)
                if (enemy.isJumpable)
                {
                    canCollide = false;
                }
            }
            // Player is sliding
            else if (isSliding)
            {
                // Can avoid collision if enemy is duckable (can slide under it)
                if (enemy.isDuckable)
                {
                    canCollide = false;
                }
            }
            
            if (canCollide && gameManager != null && !gameManager.IsInvincible())
            {
                // Mark this enemy as recently hit (using root object)
                recentlyHitEnemies.Add(rootEnemyObject);
                
                // Remove from set after cooldown period
                StartCoroutine(RemoveFromRecentlyHit(rootEnemyObject));
                
                gameManager.PlayerHit();
            }
        }
        else if (other.CompareTag("Coin"))
        {
            if (gameManager != null)
            {
                gameManager.CollectCoin();
            }
            Destroy(other.gameObject);
        }
    }
    
    /// <summary>
    /// Removes an enemy from the recently hit set after the cooldown period
    /// </summary>
    IEnumerator RemoveFromRecentlyHit(GameObject enemyObject)
    {
        yield return new WaitForSeconds(hitCooldown);
        recentlyHitEnemies.Remove(enemyObject);
    }
}
