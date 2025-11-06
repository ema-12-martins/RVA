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
        bool tracked = status.Status == Status.TRACKED ||
                       status.Status == Status.EXTENDED_TRACKED;

        if (tracked != isCurrentlyTracked)
        {
            isCurrentlyTracked = tracked;

            if (isCurrentlyTracked)
            {
                Debug.Log($"Track target '{behaviour.TargetName}' FOUND.");
                OnTrackFound?.Invoke();
            }
            else
            {
                 Debug.Log($"Track target '{behaviour.TargetName}' LOST.");
                 OnTrackLost?.Invoke();
            }
        }
    }

    void OnDestroy()
    {
        if (observer != null)
        {
            observer.OnTargetStatusChanged -= OnTargetStatusChanged;
        }
    }
}
