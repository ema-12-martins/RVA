using UnityEngine;
using UnityEngine.UI; // Required for Button
using TMPro; // Required for TextMeshProUGUI

public class CarSelectionUIManager : MonoBehaviour
{
    [Header("Change Scenes")]
    public SceneLoader sceneLoader;

    [Header("UI Elements")]
    public GameObject confirmationPanel; // Parent panel containing Text and Button
    public TextMeshProUGUI textConfirm;
    public Button buttonConfirm;

    private GameObject carPrefabToConfirm = null; // Store which car is currently targeted

    void Start()
    {
        // Ensure UI is hidden initially
        HideConfirmation();

        // Add listener to the button's click event via code
        if (buttonConfirm != null)
        {
            buttonConfirm.onClick.AddListener(OnConfirmButtonClicked);
        }
        else
        {
            Debug.LogError("Confirm Button not assigned to CarSelectionUIManager!");
        }

        if (confirmationPanel == null || textConfirm == null)
        {
             Debug.LogError("Confirmation Panel or TextConfirm not assigned to CarSelectionUIManager!");
        }
    }

    // Called by ShowHUDOnDetectCar when a target is found
    public void ShowConfirmation(GameObject carPrefab, string carName)
    {
        carPrefabToConfirm = carPrefab; // Store the prefab reference

        if (textConfirm != null)
        {
            textConfirm.text = $"Select {carName}?"; // Update text
        }
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(true); // Show the panel (and its children)
        }
    }

    // Called by ShowHUDOnDetectCar when a target is lost
    public void HideConfirmation()
    {
        carPrefabToConfirm = null; // Clear the reference
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false); // Hide the panel
        }
    }

    // Called when the single confirmation button is clicked
    private void OnConfirmButtonClicked()
    {
        if (carPrefabToConfirm != null)
        {
            GameData.selectedCarPrefab = carPrefabToConfirm;
            // --- ADD THIS LOG ---
            Debug.Log($"Car confirmed: {carPrefabToConfirm.name}. GameData prefab is now: {(GameData.selectedCarPrefab == null ? "NULL" : GameData.selectedCarPrefab.name)}");
            // --- END ADD ---
            Debug.Log($"Car confirmed: {carPrefabToConfirm.name}");
            sceneLoader.ChangeScene("Race"); // Use singleton to load next scene
        }
        else
        {
            // This case shouldn't happen if button is only active when showing confirmation,
            // but good practice to handle it.
            Debug.LogWarning("Confirm button clicked, but no car prefab was stored.");
        }
    }

    // Clean up listener when destroyed
    void OnDestroy()
    {
        if (buttonConfirm != null)
        {
            buttonConfirm.onClick.RemoveListener(OnConfirmButtonClicked);
        }
    }
}