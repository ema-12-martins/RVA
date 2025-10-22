using UnityEngine;
using TMPro;
using Vuforia;

public class ShowHUDOnDetectCar : MonoBehaviour
{
    [Header("UI References")]
    public GameObject buttonConfirm;
    public GameObject textConfirm;

    [Header("Car Prefab")]
    [Tooltip("The car prefab associated with this image target")]
    public GameObject carPrefab;

    // Reference to the Vuforia ObserverBehaviour
    private ObserverBehaviour observer;
    private TextMeshProUGUI tmpConfirmText; // Cache the TextMeshPro component

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

        if (textConfirm != null)
        {
            tmpConfirmText = textConfirm.GetComponent<TextMeshProUGUI>();
            if (tmpConfirmText == null)
            {
                Debug.LogError("TextMeshProUGUI component not found on the TextConfirm GameObject.", textConfirm);
            }
        }

        // Initially hide HUD objects
        SetHudActive(false);

        // Basic validation
        if (carPrefab == null)
        {
            Debug.LogError("Car Prefab is not assigned in the Inspector!", this);
        }
        if (buttonConfirm == null || textConfirm == null)
        {
            Debug.LogError("UI References (ButtonConfirm or TextConfirm) are not assigned!", this);
        }
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        bool isDetected = status.Status == Status.TRACKED ||
                          status.Status == Status.EXTENDED_TRACKED ||
                          status.Status == Status.LIMITED; // Consider LIMITED as potentially detectable for UI

        SetHudActive(isDetected);

        // Update confirmation text dynamically based on the prefab name
        if (isDetected && tmpConfirmText != null && carPrefab != null)
        {
            // Example: "Are you sure you want the Red Sports Car?"
            tmpConfirmText.text = $"Are you sure you want the {carPrefab.name}?";
        }
    }

    private void SetHudActive(bool isActive)
    {
        if (buttonConfirm != null) buttonConfirm.SetActive(isActive);
        if (textConfirm != null) textConfirm.SetActive(isActive);
    }

    // --- This function will be called by the Button's OnClick event ---
    public void ConfirmSelection()
    {
        if (carPrefab != null)
        {
            GameData.selectedCarPrefab = carPrefab;
            Debug.Log($"Car selected: {carPrefab.name}");

            SceneLoader.Instance.ChangeScene("Race");
        }
        else
        {
            Debug.LogError("Cannot confirm selection: Car Prefab is not set.", this);
        }
    }

    // Clean up the event subscription when the object is destroyed
    void OnDestroy()
    {
        if (observer != null)
        {
            observer.OnTargetStatusChanged -= OnTargetStatusChanged;
        }
    }
}