using UnityEngine;
using System;

public class RacerAnimator : MonoBehaviour
{
    [Header("Track Reference")]
    public TrackGenerator track; // This will be assigned by GameManager in Race scene

    [Header("Racer Settings")]
    [Tooltip("Which lane: true = left, false = right")]
    public bool leftLane = true;
    [Range(0.1f, 10f)]
    [Tooltip("Speed multiplier")]
    public float speed = 1f;
    [Range(0f, 1f)]
    [Tooltip("Starting position on track (0 to 1)")]
    public float startPosition = 0f;
    [Tooltip("Is this racer controlled by player input?")]
    public bool isPlayerControlled = false;

    [Header("Jump Settings")]
    public float jumpHeight = 1f;
    public float jumpDuration = 0.5f;
    private bool isJumping = false;
    private float jumpTimer = 0f;
    private float jumpOffset = 0.3f; // Start with base offset

    [Header("Visuals")]
    public Color racerColor = Color.green;

    private float currentPosition;
    private float previousPosition;
    private bool isInitialized = false; // Flag to check if setup is done

    public event Action OnLapCompleted;

    void Start()
    {
        Debug.Log("Hallo");
        // Defer track-dependent initialization until track is assigned
        InitializeRacer();
    }

     // Call this after the track has been assigned by GameManager
    public void InitializeRacer()
    {
        // Only initialize if track is assigned AND not already initialized
        if (track != null && !isInitialized)
        {
            currentPosition = startPosition;
            previousPosition = startPosition;
            ApplyPositionAndRotation(); // Set initial position based on track
            isInitialized = true; // Mark as initialized
            Debug.Log($"{this.name} initialized on track.");
        }
         // If called without a track assigned yet (e.g., from Start), do nothing here.
    }

    void Update()
    {
        // --- Crucial Check: Don't run update logic if not initialized ---
        if (!isInitialized || track == null)
        {
             // Try to initialize if track might have been assigned late
             if (!isInitialized) InitializeRacer();
             // If still not initialized, exit Update
             if (!isInitialized) return;
        }
        // --- End Check ---


        previousPosition = currentPosition;
        currentPosition += speed * Time.deltaTime * 0.1f;

        if (previousPosition > 0.9f && currentPosition < 0.1f)
        {
            OnLapCompleted?.Invoke();
        }

        currentPosition = Mathf.Repeat(currentPosition, 1f);

        ApplyPositionAndRotation();
        HandleJumping();
    }

    void ApplyPositionAndRotation()
    {
         // Add safety check here too, although the Update check should prevent it
         if (track == null) return;

        Vector3 targetPos = track.GetLanePosition(currentPosition, leftLane);
        float currentJumpOffset = isJumping ? jumpOffset : 0.3f;
        transform.position = targetPos + Vector3.up * currentJumpOffset;

        float lookAheadT = Mathf.Repeat(currentPosition + 0.01f, 1f);
        Vector3 lookAheadPos = track.GetLanePosition(lookAheadT, leftLane);
        Vector3 forward = (lookAheadPos - targetPos).normalized;

        if (forward != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }

    void HandleJumping()
    {
        if (isJumping)
        {
            jumpTimer += Time.deltaTime;
            float t = Mathf.Clamp01(jumpTimer / jumpDuration);
            jumpOffset = 4 * jumpHeight * t * (1 - t) + 0.3f;

            if (jumpTimer >= jumpDuration)
            {
                isJumping = false;
                jumpOffset = 0.3f;
            }
        }
        else
        {
            jumpOffset = 0.3f;
        }

        if (isPlayerControlled && !isJumping)
        {
            bool touch = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
            bool click = Input.GetMouseButtonDown(0);
            if (touch || click)
            {
                isJumping = true;
                jumpTimer = 0f;
            }
        }
    }

    // Reset position remains the same
    public void ResetPosition()
    {
        currentPosition = startPosition;
        previousPosition = startPosition;
        isJumping = false;
        jumpTimer = 0f;
        isInitialized = false; // Allow re-initialization
        InitializeRacer(); // Try to apply position immediately if track exists
    }

     // Set position remains the same
    public void SetPosition(float t)
    {
        currentPosition = Mathf.Clamp01(t);
        previousPosition = currentPosition;
        isInitialized = false; // Allow re-initialization
        InitializeRacer(); // Try to apply position immediately if track exists
    }
}