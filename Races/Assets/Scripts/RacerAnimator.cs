// RacerAnimator.cs
using UnityEngine;
using System; // Needed for Action event

public class RacerAnimator : MonoBehaviour
{
    [Header("Track Reference")]
    public TrackGenerator track;

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
    public bool isPlayerControlled = false; // New flag

    [Header("Jump Settings")]
    public float jumpHeight = 1f;
    public float jumpDuration = 0.5f;
    private bool isJumping = false;
    private float jumpTimer = 0f;
    private float jumpOffset = 0f; // Keep jump logic

    [Header("Visuals")]
    public Color racerColor = Color.green; // Keep for visual customization if needed

    private float currentPosition;
    private float previousPosition; // For lap detection
    private MeshRenderer meshRenderer;

    // Event to signal lap completion
    public event Action OnLapCompleted;

    void Start()
    {
        currentPosition = startPosition;
        previousPosition = startPosition; // Initialize previous position

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); // Or your preferred shader
            mat.color = racerColor;
            meshRenderer.material = mat;
        }

        if (track == null)
        {
            Debug.LogError("RacerAnimator: No track assigned!", this);
        }

        // Apply initial position immediately
        ApplyPositionAndRotation();
    }

    void Update()
    {
        if (track == null) return;

        // Store previous position before update
        previousPosition = currentPosition;

        // Update position along track
        currentPosition += speed * Time.deltaTime * 0.1f; // Adjust multiplier as needed

        // --- Lap Detection ---
        // Check if we crossed the finish line (wrapped around from >0.9 to <0.1, for example)
        if (previousPosition > 0.9f && currentPosition < 0.1f)
        {
            OnLapCompleted?.Invoke(); // Fire the event if subscribed
        }
        // --- End Lap Detection ---

        currentPosition = Mathf.Repeat(currentPosition, 1f); // Keep wrapping logic

        ApplyPositionAndRotation();

        HandleJumping();
    }

    void ApplyPositionAndRotation()
    {
        // Get position on the track
        Vector3 targetPos = track.GetLanePosition(currentPosition, leftLane);

        // Apply jump offset if jumping
         float currentJumpOffset = isJumping ? jumpOffset : 0.3f; // Use 0.3f base offset when not jumping

        // Apply final position
        transform.position = targetPos + Vector3.up * currentJumpOffset;

        // Calculate rotation to face forward
        float lookAheadT = Mathf.Repeat(currentPosition + 0.01f, 1f); // Ensure lookAhead wraps
        Vector3 lookAheadPos = track.GetLanePosition(lookAheadT, leftLane);
        Vector3 forward = (lookAheadPos - targetPos).normalized;

        if (forward != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }

     void HandleJumping()
    {
         // Update jump physics if currently jumping
        if (isJumping)
        {
            jumpTimer += Time.deltaTime;
            float t = Mathf.Clamp01(jumpTimer / jumpDuration);
            // Simple parabolic jump curve: y = 4h * t * (1-t)
            jumpOffset = 4 * jumpHeight * t * (1 - t) + 0.3f; // Add base offset

            if (jumpTimer >= jumpDuration)
            {
                isJumping = false;
                jumpOffset = 0.3f; // Reset to base offset
            }
        }
        else
        {
            jumpOffset = 0.3f; // Maintain base offset when not jumping
        }

         // Check for jump input ONLY if player controlled and not already jumping
        if (isPlayerControlled && !isJumping)
        {
            bool touch = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
            bool click = Input.GetMouseButtonDown(0);
            if (touch || click)
            {
                isJumping = true;
                jumpTimer = 0f;
                 // Initial jump calculation can start here if needed, or wait for next frame
            }
        }
    }

    // Reset racer to start position (might be useful later)
    public void ResetPosition()
    {
        currentPosition = startPosition;
        previousPosition = startPosition;
        isJumping = false; // Reset jump state
        jumpTimer = 0f;
        ApplyPositionAndRotation(); // Apply reset immediately
    }

    // Set position manually (might be useful later)
    public void SetPosition(float t)
    {
        currentPosition = Mathf.Clamp01(t);
        previousPosition = currentPosition; // Update previous position too
        ApplyPositionAndRotation();
    }
}