using UnityEngine;
using Vuforia;

public class CarSelection : MonoBehaviour
{
    [Header("Car Prefab")]
    [Tooltip("The car prefab associated with this image target")]
    public GameObject carPrefab;

    [Header("Manager Reference")]
    public CarSelectionUIManager uiManager;

    // Reference to the Vuforia ObserverBehaviour
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

        // Basic validation
        if (carPrefab == null)
        {
            Debug.LogError("Car Prefab is not assigned in the Inspector!", this);
        }
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool isDetected = status.Status == Status.TRACKED ||
                          status.Status == Status.EXTENDED_TRACKED ||
                          status.Status == Status.LIMITED; // Consider LIMITED as potentially detectable for UI

        if (uiManager != null)
        {
            if (isDetected)
            {
                // Tell the UI Manager to show confirmation for *this* car
                uiManager.ShowConfirmation(carPrefab, carPrefab.name); // Pass prefab and name
            }
            else
            {
                 // Check if the UI *is currently showing confirmation for this specific car*
                 // before hiding it, to prevent hiding if another target was just detected.
                 // This requires the UIManager to expose which prefab it's currently showing.
                 // (Let's keep it simple for now and just hide - refine if needed later)
                 uiManager.HideConfirmation();
            }
        }
    }

    // Clean up the event subscription when the object is destroyed
    void OnDestroy()
    {
        if (observer != null)
        {
            observer.OnTargetStatusChanged -= OnTargetStatusChanged;
        }
        // Optional: If the UI is showing *this* car when it's destroyed, hide the UI
        // if (uiManager != null && uiManager.GetCurrentPrefab() == carPrefab) {
        //     uiManager.HideConfirmation();
        // }
    }
}