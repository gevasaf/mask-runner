using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public AudioSource audioSource;

    [Header("Audio")]
    [Tooltip("Sound played when player jumps")]
    public AudioClip jumpSound;
    [Tooltip("Sound played when player slides")]
    public AudioClip slideSound;
    [Tooltip("Sound played when player moves left")]
    public AudioClip leftSound;
    [Tooltip("Sound played when player moves right")]
    public AudioClip rightSound;
    [Tooltip("Sound played when player collects a coin")]
    public AudioClip collectCoinSound;
    [Tooltip("Sound played when player gets hit (loses life)")]
    public AudioClip hitSound;
    [Tooltip("Sound played when player collects a power-up")]
    public AudioClip getPowerUpSound;

    [Header("Lane Settings")]
    public float laneWidth = 2f;
    public float laneSwitchSpeed = 10f;
    
    [Header("Movement")]
    public float jumpHeight = 2f;
    public float jumpDuration = 0.5f;
    public float slideHeight = 0.5f;
    public float slideDuration = 0.5f;

    [Header("Animator Trigger Names")]
    public string jumpTrigger = "jump";
    public string slideTrigger = "slide";
    public string leftTrigger = "left";
    public string rightTrigger = "right";
    [Tooltip("Trigger fired when player takes damage (loses life)")]
    public string hitTrigger = "hit";
    
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
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                Debug.LogWarning("Player: No Animator assigned or found on this GameObject.");
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

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
            
            if (!isJumping && !isSliding)
            {
                switch (swipe)
                {
                    case SwipeDetector.SwipeDirection.Up:
                        if (animator != null) animator.SetTrigger(jumpTrigger);
                        PlaySound(jumpSound);
                        StartCoroutine(Jump());
                        break;
                        
                    case SwipeDetector.SwipeDirection.Down:
                        if (animator != null) animator.SetTrigger(slideTrigger);
                        PlaySound(slideSound);
                        StartCoroutine(Slide());
                        break;
                        
                    case SwipeDetector.SwipeDirection.Left:
                        if (animator != null) animator.SetTrigger(leftTrigger);
                        PlaySound(leftSound);
                        ChangeLane(-1);
                        break;
                        
                    case SwipeDetector.SwipeDirection.Right:
                        if (animator != null) animator.SetTrigger(rightTrigger);
                        PlaySound(rightSound);
                        ChangeLane(1);
                        break;
                }
            }
        }
    }
    
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
    
    /// <summary>Ease-out: fast at start, slow at end (use when approaching the peak).</summary>
    static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    /// <summary>Ease-in: slow at start, fast at end (use when leaving the peak).</summary>
    static float EaseInQuad(float t)
    {
        return t * t;
    }

    IEnumerator Jump()
    {
        isJumping = true;
        float elapsed = 0f;
        float startY = transform.position.y;
        float halfDuration = jumpDuration / 2f;

        // Jump up — ease-out into the top (decelerate as we reach the peak)
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / halfDuration);
            float eased = EaseOutQuad(progress);
            float y = Mathf.Lerp(startY, startY + jumpHeight, eased);
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
            yield return null;
        }

        // Fall down — ease-in out of the top (accelerate as we approach the ground)
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / halfDuration);
            float eased = EaseInQuad(progress);
            float y = Mathf.Lerp(startY + jumpHeight, originalY, eased);
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
                
                if (animator != null) animator.SetTrigger(hitTrigger);
                PlaySound(hitSound);
                gameManager.PlayerHit();
            }
        }
        else if (other.CompareTag("Coin"))
        {
            if (gameManager != null)
            {
                gameManager.CollectCoin();
            }
            PlaySound(collectCoinSound);
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
    
    /// <summary>
    /// Plays a sound effect if the audio clip is assigned
    /// </summary>
    void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    /// <summary>
    /// Called when player collects a power-up. Plays the power-up sound.
    /// </summary>
    public void OnPowerUpCollected()
    {
        PlaySound(getPowerUpSound);
    }
}
