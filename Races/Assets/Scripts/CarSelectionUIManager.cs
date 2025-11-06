using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CarSelectionUIManager : MonoBehaviour
{
    [Header("Change Scenes")]
    public SceneLoader sceneLoader;

    [Header("UI Elements")]
    public GameObject confirmationPanel;
    public TextMeshProUGUI textConfirm;
    public Button buttonConfirm;

    private GameObject carPrefabToConfirm = null;

    void Start()
    {
        HideConfirmation();

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
        carPrefabToConfirm = carPrefab;

        if (textConfirm != null)
        {
            textConfirm.text = $"Select {carName}?";
        }
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(true);
        }
    }

    // Called by ShowHUDOnDetectCar when a target is lost
    public void HideConfirmation()
    {
        carPrefabToConfirm = null;
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }
    }

    // Called when the single confirmation button is clicked
    private void OnConfirmButtonClicked()
    {
        if (carPrefabToConfirm != null)
        {
            GameData.selectedCarPrefab = carPrefabToConfirm;
            sceneLoader.ChangeScene("TrackPlanning");
        }
        else
        {
            Debug.LogWarning("Confirm button clicked, but no car prefab was stored.");
        }
    }

    void OnDestroy()
    {
        if (buttonConfirm != null)
        {
            buttonConfirm.onClick.RemoveListener(OnConfirmButtonClicked);
        }
    }
}