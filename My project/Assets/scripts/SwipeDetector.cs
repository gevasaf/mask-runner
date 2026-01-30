using UnityEngine;

public class SwipeDetector : MonoBehaviour
{
    private Vector2 touchStartPos;
    private Vector2 touchEndPos;
    private bool isTouching = false;
    
    public float minSwipeDistance = 50f;
    
    public enum SwipeDirection
    {
        None,
        Up,
        Down,
        Left,
        Right
    }
    
    private SwipeDirection currentSwipe = SwipeDirection.None;
    
    void Update()
    {
        currentSwipe = SwipeDirection.None;

#if UNITY_EDITOR
        // Keyboard input for development
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space))
            currentSwipe = SwipeDirection.Up;
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            currentSwipe = SwipeDirection.Down;
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            currentSwipe = SwipeDirection.Left;
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            currentSwipe = SwipeDirection.Right;
#endif

        // Mobile touch input
        if (currentSwipe == SwipeDirection.None && Input.touchCount > 0)
        {
            Touch touch = Input.touches[0];
            
            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos = touch.position;
                isTouching = true;
            }
            else if (touch.phase == TouchPhase.Ended && isTouching)
            {
                touchEndPos = touch.position;
                currentSwipe = DetectSwipe();
                isTouching = false;
            }
        }
        // Mouse input for testing
        if (currentSwipe == SwipeDirection.None && Input.GetMouseButtonDown(0))
        {
            touchStartPos = Input.mousePosition;
            isTouching = true;
        }
        if (Input.GetMouseButtonUp(0) && isTouching)
        {
            if (currentSwipe == SwipeDirection.None)
            {
                touchEndPos = Input.mousePosition;
                currentSwipe = DetectSwipe();
            }
            isTouching = false;
        }
    }
    
    private SwipeDirection DetectSwipe()
    {
        Vector2 swipeVector = touchEndPos - touchStartPos;
        float swipeDistance = swipeVector.magnitude;
        
        if (swipeDistance < minSwipeDistance)
            return SwipeDirection.None;
        
        float swipeAngle = Mathf.Atan2(swipeVector.y, swipeVector.x) * Mathf.Rad2Deg;
        
        // Up swipe (45 to 135 degrees)
        if (swipeAngle > 45 && swipeAngle <= 135)
            return SwipeDirection.Up;
        // Down swipe (-135 to -45 degrees)
        else if (swipeAngle > -135 && swipeAngle <= -45)
            return SwipeDirection.Down;
        // Right swipe (-45 to 45 degrees)
        else if (swipeAngle > -45 && swipeAngle <= 45)
            return SwipeDirection.Right;
        // Left swipe (135 to 180 or -180 to -135 degrees)
        else
            return SwipeDirection.Left;
    }
    
    public SwipeDirection GetSwipe()
    {
        SwipeDirection swipe = currentSwipe;
        currentSwipe = SwipeDirection.None; // Reset after reading
        return swipe;
    }
    
    public bool HasSwipe()
    {
        return currentSwipe != SwipeDirection.None;
    }
}
