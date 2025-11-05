// LapCounter.cs - NEW FILE
// Robust lap counting using checkpoint system
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
    private bool turned = false;
    
    public event Action OnLapCompleted;

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
            // Set default checkpoints if none are defined
            checkpoints = new float[] { 0.25f, 0.5f, 0.75f, 0.95f };
        }
        
        checkpointsPassed = new bool[checkpoints.Length];
        ResetCheckpoints();
        isInitialized = true;
        turned = false;
    }

    public void UpdatePosition(float normalizedPosition)
    {
        // Check if we crossed the finish line (wrap around from high to low)
        bool crossedFinishLine = lastPosition > 0.9f && normalizedPosition < 0.1f;
        
        if (crossedFinishLine)
        {
            // Only count lap if all checkpoints were passed
            if (AllCheckpointsPassed())
            {
                totalLaps++;
                Debug.Log($"{gameObject.name} completed lap {totalLaps} at position {normalizedPosition:F3}");
                OnLapCompleted?.Invoke();
                ResetCheckpoints();
            }
            else
            {
                Debug.LogWarning($"{gameObject.name} crossed finish line but missed checkpoints! " +
                                $"Passed: {GetCheckpointStatus()}");
                ResetCheckpoints(); // Reset anyway to prevent getting stuck
            }
        }
        
        // Check current checkpoint
        if (currentCheckpointIndex < checkpoints.Length)
        {
            float targetCheckpoint = checkpoints[currentCheckpointIndex];
            
            // Check if we're within tolerance of the checkpoint
            if (Mathf.Abs(normalizedPosition - targetCheckpoint) <= checkpointTolerance)
            {
                if (!checkpointsPassed[currentCheckpointIndex])
                {
                    checkpointsPassed[currentCheckpointIndex] = true;
                    Debug.Log($"{gameObject.name} passed checkpoint {currentCheckpointIndex + 1}/{checkpoints.Length} " +
                             $"at position {normalizedPosition:F3}");
                    currentCheckpointIndex++;
                }
            }

            //Verify use the shortcut
            if (normalizedPosition >= 0.35f && normalizedPosition <= 0.40f)
            {
                float tilt = Input.acceleration.x;
                float tiltThreshold = 0.3f;

                if (Mathf.Abs(tilt) > tiltThreshold)
                {
                    Debug.Log("O jogador rodou o telemóvel na zona especial!");
                    GameManager.selected_track = (GameManager.selected_track == 1 ? 2 : 1);
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
            Initialize(); // Ensure initialization
            if (checkpointsPassed == null) return; // Safety check
        }
        
        for (int i = 0; i < checkpointsPassed.Length; i++)
        {
            checkpointsPassed[i] = false;
        }
        currentCheckpointIndex = 0;

        //To follow by default the big route
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
        Initialize(); // Ensure initialization before reset
        totalLaps = 0;
        ResetCheckpoints();
        lastPosition = 0f;
    }

    // Optional: Visualize checkpoints in editor
    void OnDrawGizmos()
    {
        if (checkpoints == null || checkpoints.Length == 0) return;
        
        // Try to find track generator
        TrackGenerator track = FindAnyObjectByType<TrackGenerator>();
        if (track == null) return;

        // Draw checkpoint positions
        Gizmos.color = Color.yellow;
        foreach (float checkpoint in checkpoints)
        {
            Vector3 pos = track.GetTrackPosition(checkpoint);
            Gizmos.DrawWireSphere(pos + Vector3.up * 2f, 0.5f);
        }

        // Draw finish line
        Gizmos.color = Color.green;
        Vector3 finishPos = track.GetTrackPosition(0f);
        Gizmos.DrawWireSphere(finishPos + Vector3.up * 2f, 0.7f);
    }
}