using UnityEngine;
using Vuforia;
using System;

public class TrackTargetHandler : MonoBehaviour
{
    public event Action OnTrackFound;
    public event Action OnTrackLost;

    private ObserverBehaviour observer;
    private bool isCurrentlyTracked = false;

    void Start()
    {
        observer = GetComponent<ObserverBehaviour>();
        if (observer != null)
        {
            observer.OnTargetStatusChanged += OnTargetStatusChanged;
             Debug.Log($"TrackTargetHandler initialized for target: {observer.TargetName}");
             // Initial check in case target is already visible on start
             OnTargetStatusChanged(observer, observer.TargetStatus);
        }
        else
        {
            Debug.LogError("ObserverBehaviour not found on the Track ImageTarget GameObject!", this);
        }
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        // Determine if the target is considered "found" (TRACKED or EXTENDED_TRACKED)
        bool tracked = status.Status == Status.TRACKED ||
                       status.Status == Status.EXTENDED_TRACKED;

        // Check if the tracking status has changed since the last update
        if (tracked != isCurrentlyTracked)
        {
            isCurrentlyTracked = tracked; // Update the current status

            if (isCurrentlyTracked)
            {
                Debug.Log($"Track target '{behaviour.TargetName}' FOUND.");
                OnTrackFound?.Invoke(); // Fire the found event
            }
            else
            {
                 Debug.Log($"Track target '{behaviour.TargetName}' LOST.");
                 OnTrackLost?.Invoke(); // Fire the lost event
            }
        }
         // Log detailed status if needed
         // Debug.Log($"Target: {behaviour.TargetName}, Status: {status.Status}, StatusInfo: {status.StatusInfo}");
    }

    // Clean up the event subscription when the object is destroyed
    void OnDestroy()
    {
        if (observer != null)
        {
            observer.OnTargetStatusChanged -= OnTargetStatusChanged;
        }
    }

     // Public property to check current status if needed elsewhere
     public bool IsTracked => isCurrentlyTracked;
}
