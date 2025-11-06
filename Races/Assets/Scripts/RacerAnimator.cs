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
    public bool isPlayer; // If not player, it's a bot

    [Header("Racer Settings")]
    [Tooltip("Which lane: true = left, false = right")]
    public bool leftLane = true;

    [Range(0.1f, 10f)]
    [Tooltip("Speed multiplier (units per second)")]
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

    [Header("Bot Shortcut Decision")]
    private bool botShortcutDecidedThisLap = false;

    private float currentPosition;
    private bool isInitialized = false;
    private LapCounter lapCounter;

    // Forwarded event
    public event Action OnLapCompleted;

    public List<ItemEffect> activeEffects = new List<ItemEffect>();

    // NEW: shortcut state
    private bool isInShortcut = false;
    private float shortcutT = 0f; // normalized along the open shortcut (0 -> 1)
    private float mainPosBeforeShortcut = 0f;

    void Awake()
    {
        lapCounter = GetComponent<LapCounter>();
        if (lapCounter == null)
        {
            lapCounter = gameObject.AddComponent<LapCounter>();
        }

        lapCounter.OnLapCompleted += HandleLapCompleted;

        // Subscribe to shortcut request from LapCounter
        lapCounter.OnShortcutEnterRequested += HandleShortcutEnterRequested;
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
                lapCounter.lapCountingPaused = false;
            }

            isInShortcut = false;
            shortcutT = 0f;

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

        if (isInShortcut)
        {
            // Move along the (open) shortcut from t=0 -> t=1
            float shortcutLength = shortcut.GetTrackLength();
            float shortcutNormSpeed = speed / shortcutLength;
            shortcutT += shortcutNormSpeed * Time.deltaTime;
            shortcutT = Mathf.Clamp01(shortcutT);

            // Position/orientation from shortcut
            Vector3 targetPos = shortcut.GetLanePosition(shortcutT, leftLane);
            float currentJumpOffset = isJumping ? jumpOffset : 0.3f;
            transform.position = targetPos + Vector3.up * currentJumpOffset;

            float lookAheadT = Mathf.Clamp01(shortcutT + 0.01f);
            Vector3 lookAheadPos = shortcut.GetLanePosition(lookAheadT, leftLane);
            Vector3 forward = (lookAheadPos - targetPos).normalized;
            if (forward != Vector3.zero)
                transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

            HandleJumping();

            // Reached end of shortcut: rejoin main track
            if (shortcutT >= 1f - Mathf.Epsilon)
            {
                Vector3 exitWorld = shortcut.GetLanePosition(1f, leftLane);
                float rejoinT = track.FindClosestT(exitWorld, 768);

                // Sync checkpoints that would have been crossed between mainPosBeforeShortcut -> rejoinT
                if (lapCounter != null)
                {
                    lapCounter.SyncCheckpointsBetween(mainPosBeforeShortcut, rejoinT);
                    lapCounter.lapCountingPaused = false; // resume counting
                }

                currentPosition = rejoinT; // resume movement on main at nearest point
                isInShortcut = false;
                GameManager.selected_track = 1; // keep consistent for debug/other systems
            }

            return; // skip main-track motion in this frame
        }

        // ----- NORMAL MAIN-TRACK MOVEMENT -----
        TrackGenerator currentTrack = GetCurrentTrack();
        float trackLength = currentTrack.GetTrackLength();
        float normalizedSpeed = speed / trackLength;

        currentPosition += normalizedSpeed * Time.deltaTime;
        currentPosition = Mathf.Repeat(currentPosition, 1f);

        // Update lap counter only while on main
        if (lapCounter != null)
        {
            lapCounter.UpdatePosition(currentPosition);
        }

        // BOT shortcut decision: only decide inside the same zone as player, once per lap
        if (!isPlayer && !isInShortcut)
        {
            // Must match LapCounter's decision window (0.35–0.40)
            bool inShortcutZone = currentPosition >= 0.35f && currentPosition <= 0.40f;

            if (inShortcutZone && !botShortcutDecidedThisLap)
            {
                botShortcutDecidedThisLap = true; // decide once per lap when entering the zone

                // Keep using the exact same probability you had before
                if (UnityEngine.Random.value < GameData.probabilityOfOvercomingObstacles)
                {
                    EnterShortcutNow(); // go through the open-shortcut path (pauses lap counting, etc.)
                }
                // else: skip this lap and continue on main track
            }
        }

        ApplyPositionAndRotation();
        HandleJumping();
    }

    TrackGenerator GetCurrentTrack()
    {
        if (isInShortcut) return shortcut;
        return track;
    }

    void ApplyPositionAndRotation()
    {
        if (track == null || shortcut == null) return;

        TrackGenerator currentTrack = GetCurrentTrack();

        Vector3 targetPos = currentTrack.GetLanePosition(currentPosition, leftLane);
        float currentJumpOffset = isJumping ? jumpOffset : 0.3f;
        transform.position = targetPos + Vector3.up * currentJumpOffset;

        float lookAheadT = Mathf.Repeat(currentPosition + 0.01f, 1f);
        Vector3 lookAheadPos = currentTrack.GetLanePosition(lookAheadT, leftLane);
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
            float accelY = Input.acceleration.y;
            // Debug.Log("Acceleration Y: " + accelY);

            if (accelY < -1.0f)
            {
                isJumping = true;
                jumpTimer = 0f;
                // Debug.Log("Jump detected!");
            }
        }
    }

    void HandleLapCompleted()
    {
        OnLapCompleted?.Invoke();
        botShortcutDecidedThisLap = false;
    }

    private void HandleShortcutEnterRequested()
    {
        // Only the player can enter shortcut via tilt
        if (isPlayer)
            EnterShortcutNow();
    }

    // Enter shortcut at the beginning (t = 0)
    public void EnterShortcutNow()
    {
        if (isInShortcut) return;

        isInShortcut = true;
        shortcutT = 0f;
        mainPosBeforeShortcut = currentPosition;

        if (lapCounter != null)
            lapCounter.lapCountingPaused = true;

        GameManager.selected_track = 2; // for consistency/debug visibility
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
            lapCounter.lapCountingPaused = false;
        }

        isInShortcut = false;
        shortcutT = 0f;
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
            lapCounter.OnShortcutEnterRequested -= HandleShortcutEnterRequested;
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
