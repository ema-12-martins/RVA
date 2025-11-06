using UnityEngine;
using System;
using UnityEngine.InputSystem;

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

    public event Action OnShortcutEnterRequested;

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
        if (lapCountingPaused) return; // Ignore updates while on shortcut

        bool crossedFinishLine = lastPosition > 0.9f && normalizedPosition < 0.1f;

        if (crossedFinishLine)
        {
            if (AllCheckpointsPassed())
            {
                totalLaps++;
                OnLapCompleted?.Invoke();
                ResetCheckpoints();
            }
            else
            {
                ResetCheckpoints(); // Prevent getting stuck
            }
        }

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

            // Request enter shortcut on tilt
            if (inShortcutZone)
            {
                float tilt = LinearAccelerationSensor.current.acceleration.ReadValue().x;
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

        lastPosition = toPos;
    }

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
    }
}
