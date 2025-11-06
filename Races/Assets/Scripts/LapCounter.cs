// LapCounter.cs - Modified version for shortcut pausing & checkpoint sync
using UnityEngine;
using System;

public class LapCounter : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    [Tooltip("Position thresholds on track (0-1) that must be crossed in order")]
    public float[] checkpoints = new float[] { 0.25f, 0.5f, 0.75f, 0.95f }; // Finish line at ~0/1

    [Tooltip("How close racer must be to checkpoint to trigger it")]
    [Range(0.01f, 0.1f)]
    public float checkpointTolerance = 0.05f;

    private int currentCheckpointIndex = 0;
    private bool[] checkpointsPassed;
    private int totalLaps = 0;
    private float lastPosition = 0f;
    private bool isInitialized = false;

    public event Action OnLapCompleted;

    // NEW: request entering the shortcut (RacerAnimator listens)
    public event Action OnShortcutEnterRequested;

    // NEW: while on shortcut we pause lap counting logic entirely
    [NonSerialized] public bool lapCountingPaused = false;

    private bool wasInShortcutZone = false;

    void Awake()
    {
        Initialize();
    }

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        if (isInitialized) return;

        if (checkpoints == null || checkpoints.Length == 0)
        {
            checkpoints = new float[] { 0.25f, 0.5f, 0.75f, 0.95f };
        }

        checkpointsPassed = new bool[checkpoints.Length];
        ResetCheckpoints();
        isInitialized = true;
    }

    public void UpdatePosition(float normalizedPosition)
    {
        if (!isInitialized) Initialize();
        if (lapCountingPaused) return; // << NEW: ignore updates while on shortcut

        // Check if we crossed the finish line (wrap around from high to low)
        bool crossedFinishLine = lastPosition > 0.9f && normalizedPosition < 0.1f;

        if (crossedFinishLine)
        {
            // Only count lap if all checkpoints were passed
            if (AllCheckpointsPassed())
            {
                totalLaps++;
                OnLapCompleted?.Invoke();
                ResetCheckpoints();
            }
            else
            {
                ResetCheckpoints(); // Reset anyway to prevent getting stuck
            }
        }

        // Check current checkpoint
        if (currentCheckpointIndex < checkpoints.Length)
        {
            float targetCheckpoint = checkpoints[currentCheckpointIndex];

            if (Mathf.Abs(normalizedPosition - targetCheckpoint) <= checkpointTolerance)
            {
                if (!checkpointsPassed[currentCheckpointIndex])
                {
                    checkpointsPassed[currentCheckpointIndex] = true;
                    currentCheckpointIndex++;
                }
            }

            // --- Shortcut decision area ---
            bool inShortcutZone = normalizedPosition >= 0.35f && normalizedPosition <= 0.4f;

            if (inShortcutZone && !wasInShortcutZone)
            {
                Debug.Log($"[{name}] ENTERED shortcut zone at normalizedPosition={normalizedPosition:F3}");
            }
            else if (!inShortcutZone && wasInShortcutZone)
            {
                Debug.Log($"[{name}] EXITED shortcut zone at normalizedPosition={normalizedPosition:F3}");
            }

            wasInShortcutZone = inShortcutZone;

            // Request enter shortcut on tilt
            if (inShortcutZone)
            {
                float tilt = Input.acceleration.x;
                const float tiltThreshold = 0.3f;
                if (Mathf.Abs(tilt) > tiltThreshold)
                {
                    Debug.Log($"[{name}] Requested ENTER SHORTCUT (tilt={tilt:F2}, normPos={normalizedPosition:F3})");
                    OnShortcutEnterRequested?.Invoke();
                }
            }
        }

        lastPosition = normalizedPosition;
    }

    bool AllCheckpointsPassed()
    {
        foreach (bool passed in checkpointsPassed)
        {
            if (!passed) return false;
        }
        return true;
    }

    void ResetCheckpoints()
    {
        if (checkpointsPassed == null || checkpointsPassed.Length == 0)
        {
            Initialize();
            if (checkpointsPassed == null) return;
        }

        for (int i = 0; i < checkpointsPassed.Length; i++)
        {
            checkpointsPassed[i] = false;
        }
        currentCheckpointIndex = 0;

        // Default behavior: start each lap following main route
        GameManager.selected_track = 1;
    }

    string GetCheckpointStatus()
    {
        string status = "";
        for (int i = 0; i < checkpointsPassed.Length; i++)
        {
            status += $"CP{i + 1}:{(checkpointsPassed[i] ? "✓" : "✗")} ";
        }
        return status;
    }

    public int GetTotalLaps()
    {
        return totalLaps;
    }

    public void ResetLaps()
    {
        Initialize();
        totalLaps = 0;
        ResetCheckpoints();
        lastPosition = 0f;
    }

    // NEW: mark checkpoints crossed between two normalized positions (handles wrap)
    public void SyncCheckpointsBetween(float fromPos, float toPos)
    {
        if (checkpoints == null || checkpoints.Length == 0) return;

        bool wrapped = toPos < fromPos;

        Func<float, bool> isBetween = cp =>
        {
            if (!wrapped)
                return cp >= fromPos && cp <= toPos;
            else
                return (cp >= fromPos && cp <= 1f) || (cp >= 0f && cp <= toPos);
        };

        for (int i = 0; i < checkpoints.Length; i++)
        {
            if (!checkpointsPassed[i] && isBetween(checkpoints[i]))
            {
                checkpointsPassed[i] = true;
                currentCheckpointIndex = Mathf.Max(currentCheckpointIndex, i + 1);
            }
        }

        // Align lastPosition to prevent false wrap detection
        lastPosition = toPos;
    }

    // Optional: Visualize checkpoints in editor
    void OnDrawGizmos()
    {
        if (checkpoints == null || checkpoints.Length == 0) return;

        TrackGenerator track = FindAnyObjectByType<TrackGenerator>();
        if (track == null) return;

        Gizmos.color = Color.yellow;
        foreach (float checkpoint in checkpoints)
        {
            Vector3 pos = track.GetTrackPosition(checkpoint);
            Gizmos.DrawWireSphere(pos + Vector3.up * 2f, 0.5f);
        }

        Gizmos.color = Color.green;
        Vector3 finishPos = track.GetTrackPosition(0f);
        Gizmos.DrawWireSphere(finishPos + Vector3.up * 2f, 0.7f);

        // Shortcut zone visualization
        Gizmos.color = new Color(0.1f, 0.8f, 1f, 0.8f);
        Vector3 z1 = track.GetTrackPosition(0.15f);
        Vector3 z2 = track.GetTrackPosition(0.2f);
        Gizmos.DrawWireSphere(z1 + Vector3.up * 1.5f, 0.3f);
        Gizmos.DrawWireSphere(z2 + Vector3.up * 1.5f, 0.3f);
    }
}
