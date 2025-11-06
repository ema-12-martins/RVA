using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveTrackAndGoButton : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] TrackGenerator track;
    [SerializeField] Transform placedObjectsParent;
    [SerializeField] Button saveAndGoButton;

    [Header("Navigation")]
    [SerializeField] string raceSceneName = "Race";
    [SerializeField] SceneLoader sceneLoader;

    [Header("Feedback (optional)")]
    [SerializeField] TMPro.TMP_Text statusText;

    void Awake()
    {
        if (saveAndGoButton != null)
            saveAndGoButton.onClick.AddListener(OnSaveAndGo);
    }

    void OnDestroy()
    {
        if (saveAndGoButton != null)
            saveAndGoButton.onClick.RemoveListener(OnSaveAndGo);
    }

    void OnSaveAndGo()
    {
        if (track == null)
        {
            SetStatus("No TrackGenerator assigned.");
            return;
        }

        var save = new GameData.TrackSaveData();

        // Save parametric placement data from metadata components
        save.objects = new List<GameData.PlacedObjectData>();
        if (placedObjectsParent != null)
        {
            int savedCount = 0;
            for (int i = 0; i < placedObjectsParent.childCount; i++)
            {
                var child = placedObjectsParent.GetChild(i);
                var meta = child.GetComponent<PlacedObjectMetadata>();
                
                if (meta != null)
                {
                    var pod = new GameData.PlacedObjectData
                    {
                        prefabName = meta.prefabName,
                        t = meta.t,
                        isLeftLane = meta.isLeftLane,
                        yawOffset = meta.yawOffset
                    };
                    save.objects.Add(pod);
                    savedCount++;
                }
                else
                {
                    Debug.LogWarning($"Object {child.name} missing PlacedObjectMetadata component. Skipping.");
                }
            }
            SetStatus($"Saved {savedCount} object(s). Loading Race…");
        }
        else
        {
            SetStatus("No placed objects parent. Loading Race…");
        }

        GameData.BuiltTrack = save;

        if (sceneLoader != null) sceneLoader.ChangeScene(raceSceneName);
        else UnityEngine.SceneManagement.SceneManager.LoadScene(raceSceneName);
    }

    void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
        else Debug.Log(msg);
    }
}