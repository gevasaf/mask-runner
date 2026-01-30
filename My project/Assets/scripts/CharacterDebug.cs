using System.Collections;
using UnityEngine;

public class CharacterDebug : MonoBehaviour
{
    [Header("References")]
    public Animator animator;

    [Header("Jump Raise")]
    public float jumpHeight = 2f;
    public float jumpDuration = 0.5f;

    [Header("Animator Trigger Names")]
    public string jumpTrigger = "jump";
    public string slideTrigger = "slide";
    public string rightTrigger = "right";
    public string leftTrigger = "left";

    private SwipeDetector swipeDetector;
    private float originalY;
    private bool isJumping = false;

    void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                Debug.LogWarning("CharacterDebug: No Animator assigned or found on this GameObject.");
        }

        swipeDetector = FindObjectOfType<SwipeDetector>();
        if (swipeDetector == null)
        {
            GameObject swipeObj = new GameObject("SwipeDetector");
            swipeDetector = swipeObj.AddComponent<SwipeDetector>();
        }

        originalY = transform.position.y;
    }

    void Update()
    {
        if (animator == null || !swipeDetector.HasSwipe())
            return;

        SwipeDetector.SwipeDirection swipe = swipeDetector.GetSwipe();

        switch (swipe)
        {
            case SwipeDetector.SwipeDirection.Up:
                animator.SetTrigger(jumpTrigger);
                if (!isJumping)
                    StartCoroutine(JumpRoutine());
                break;
            case SwipeDetector.SwipeDirection.Down:
                animator.SetTrigger(slideTrigger);
                break;
            case SwipeDetector.SwipeDirection.Right:
                animator.SetTrigger(rightTrigger);
                break;
            case SwipeDetector.SwipeDirection.Left:
                animator.SetTrigger(leftTrigger);
                break;
        }
    }

    IEnumerator JumpRoutine()
    {
        isJumping = true;
        float startY = transform.position.y;
        float halfDuration = jumpDuration / 2f;

        // Go up
        for (float t = 0f; t < halfDuration; t += Time.deltaTime)
        {
            float progress = t / halfDuration;
            float y = Mathf.Lerp(startY, startY + jumpHeight, progress);
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
            yield return null;
        }

        // Come down
        float peakY = startY + jumpHeight;
        for (float t = 0f; t < halfDuration; t += Time.deltaTime)
        {
            float progress = t / halfDuration;
            float y = Mathf.Lerp(peakY, originalY, progress);
            transform.position = new Vector3(transform.position.x, y, transform.position.z);
            yield return null;
        }

        transform.position = new Vector3(transform.position.x, originalY, transform.position.z);
        isJumping = false;
    }
}
