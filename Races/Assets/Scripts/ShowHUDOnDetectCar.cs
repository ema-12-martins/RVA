using UnityEngine;
using TMPro;
using Vuforia;
using System.Drawing;

public class ShowHUDOnDetectCar : MonoBehaviour
{
    // References to the objects we want to show or hide
    public GameObject buttonConfirm;
    public GameObject TextConfirm;
    public string displayText;
    public string color;

    // Reference to the Vuforia ObserverBehaviour
    private ObserverBehaviour observer;

    void Start()
    {
        // Get the ObserverBehaviour attached to this GameObject
        observer = GetComponent<ObserverBehaviour>();

        // Subscribe to the target status changed event
        if (observer != null)
        {
            observer.OnTargetStatusChanged += OnTargetStatusChanged;
        }

        // Initially hide HUD objects
        if (buttonConfirm != null) buttonConfirm.SetActive(false);
        if (TextConfirm != null) TextConfirm.SetActive(false);
    }

    // Called whenever the target's tracking status changes
    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        // Check if the target is detected
        bool isDetected =
            status.Status == Status.TRACKED ||
            status.Status == Status.EXTENDED_TRACKED;

        // Update text safely
        if (TextConfirm != null)
        {
            TextMeshProUGUI tmp = TextConfirm.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = displayText;

            TextConfirm.SetActive(isDetected);
        }

        //Put button visible
        if (buttonConfirm != null) buttonConfirm.SetActive(isDetected);

        // Set the global variable
        GameData.carColor = color;
        // Print the color to the Console
        Debug.Log("Car color set to: " + GameData.carColor);


    }
}
