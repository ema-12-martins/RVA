using UnityEngine;
using Vuforia;

public class CarSelection : MonoBehaviour
{
    [Header("Car Prefab")]
    [Tooltip("The car prefab associated with this image target")]
    public GameObject carPrefab;

    [Header("Manager Reference")]
    public CarSelectionUIManager uiManager;

    private ObserverBehaviour observer;

    void Start()
    {
        observer = GetComponent<ObserverBehaviour>();
        if (observer != null)
        {
            observer.OnTargetStatusChanged += OnTargetStatusChanged;
        }
        else
        {
            Debug.LogError("ObserverBehaviour not found on this GameObject.", this);
        }

        if (uiManager == null)
        {
            uiManager = FindAnyObjectByType<CarSelectionUIManager>();
            if (uiManager == null)
            {
                Debug.LogError("CarSelectionUIManager not found in the scene!", this);
            }
        }

        if (carPrefab == null)
        {
            Debug.LogError("Car Prefab is not assigned in the Inspector!", this);
        }
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool isDetected = status.Status == Status.TRACKED;

        if (uiManager != null)
        {
            if (isDetected)
            {
                uiManager.ShowConfirmation(carPrefab, carPrefab.name);
            }
            else
            {
                 uiManager.HideConfirmation();
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