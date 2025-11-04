using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(LapCounter))] // Ensure LapCounter is always present
public class RacerAnimator : MonoBehaviour
{
    [Header("Track Reference")]
    public TrackGenerator track;

    [Header("Shortcut Reference")]
    public TrackGenerator shortcut;
    public bool isPlayer; //If is not the player, is the bot -> Can't use the shortcut

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
    private float jumpOffset = 0.3f;

    private float currentPosition;
    private bool isInitialized = false;
    private LapCounter lapCounter;

    // This event is now triggered by LapCounter
    public event Action OnLapCompleted;

    public List<ItemEffect> activeEffects = new List<ItemEffect>();

    void Awake()
    {
        // Get or add LapCounter component
        lapCounter = GetComponent<LapCounter>();
        if (lapCounter == null)
        {
            lapCounter = gameObject.AddComponent<LapCounter>();
        }
        
        // Subscribe to lap counter's event
        lapCounter.OnLapCompleted += HandleLapCompleted;
    }

    void Start()
    {
        InitializeRacer();
    }

    public void InitializeRacer()
    {
        if (track != null && shortcut != null && !isInitialized)
        {
            currentPosition = startPosition;
            ApplyPositionAndRotation();
            isInitialized = true;
            GameManager.selected_track = 1;

            // Reset lap counter when initializing
            if (lapCounter != null)
            {
                lapCounter.ResetLaps();
            }
            
            Debug.Log($"{this.name} initialized on track at position {startPosition:F3}");
        }
    }

    void Update()
    {
        if (!isInitialized || track == null || shortcut == null)
        {
            if (!isInitialized) InitializeRacer();
            if (!isInitialized) return;
        }

        currentPosition += speed * Time.deltaTime * 0.1f;
        currentPosition = Mathf.Repeat(currentPosition, 1f);

        // Update lap counter with current position
        if (lapCounter != null)
        {
            lapCounter.UpdatePosition(currentPosition);
        }

        ApplyPositionAndRotation();
        HandleJumping();
    }

    void ApplyPositionAndRotation()
    {
        if (track == null || shortcut == null) return;

        if (GameManager.selected_track == 2  && isPlayer == true)
        {

            Vector3 targetPos = shortcut.GetLanePosition(currentPosition, leftLane);
            float currentJumpOffset = isJumping ? jumpOffset : 0.3f;
            transform.position = targetPos + Vector3.up * currentJumpOffset;

            float lookAheadT = Mathf.Repeat(currentPosition + 0.01f, 1f);
            Vector3 lookAheadPos = shortcut.GetLanePosition(lookAheadT, leftLane);
            Vector3 forward = (lookAheadPos - targetPos).normalized;

            if (forward != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            }
        }
        else
        {
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
            //Get acceleration
            float accelY = Input.acceleration.y;
            Debug.Log("Acceleration Y: " + accelY);

            //If it's above the limit, it jumps
            if (accelY < -1.0f)
            {
                isJumping = true;
                jumpTimer = 0f;
                Debug.Log("Salto detectado!");
            }
        }
    }

    void HandleLapCompleted()
    {
        // Forward the lap counter's event to subscribers
        OnLapCompleted?.Invoke();
    }

    public void ResetPosition()
    {
        currentPosition = startPosition;
        isJumping = false;
        jumpTimer = 0f;
        isInitialized = false;
        
        if (lapCounter != null)
        {
            lapCounter.ResetLaps();
        }
        
        InitializeRacer();
    }

    public void SetPosition(float t)
    {
        currentPosition = Mathf.Clamp01(t);
        isInitialized = false;
        InitializeRacer();
    }

    public float GetCurrentPosition()
    {
        return currentPosition;
    }

    public int GetLapCount()
    {
        return lapCounter != null ? lapCounter.GetTotalLaps() : 0;
    }

    void OnDestroy()
    {
        if (lapCounter != null)
        {
            lapCounter.OnLapCompleted -= HandleLapCompleted;
        }
    }

    public void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
    }

    public void AddEffect(ItemEffect effect)
    {
        if (effect != null)
        {
            activeEffects.Add(effect);
            effect.ApplyEffect(gameObject, effect.dir);

            if (effect.duration > 0f)
            {
                StartCoroutine(RemoveEffect(effect, effect.duration));
            }
        }
    }

    private IEnumerator RemoveEffect(ItemEffect effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (activeEffects.Contains(effect))
        {
            effect.RemoveEffect(gameObject, effect.dir);
            activeEffects.Remove(effect);
        }
    }
}