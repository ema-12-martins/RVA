using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveTrackAndGoButton : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] TrackGenerator track;           // your existing generator on the track root
    [SerializeField] Transform placedObjectsParent;  // trackRoot/PlacedObjects
    [SerializeField] Button saveAndGoButton;         // the UI Button in this scene

    [Header("Navigation")]
    [SerializeField] string raceSceneName = "Race";  // target scene name
    [SerializeField] SceneLoader sceneLoader;        // optional: if you have one in scene

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

        if (track.controlPoints != null && track.controlPoints.Length > 0)
        {
            var cps = track.controlPoints;
            var pos = new Vector3[cps.Length];
            for (int i = 0; i < cps.Length; i++)
            {
                if (cps[i] == null)
                {
                    SetStatus($"Control point {i} is null; skipping.");
                    pos[i] = Vector3.zero;
                }
                else
                {
                    pos[i] = cps[i].position;
                }
            }
            save.controlPoints = pos;
        }
        else
        {
            save.controlPoints = new Vector3[0];
        }

        // Placed objects (unchanged)
        save.objects = new List<GameData.PlacedObjectData>();
        if (placedObjectsParent != null)
        {
            for (int i = 0; i < placedObjectsParent.childCount; i++)
            {
                var child = placedObjectsParent.GetChild(i);
                var pod = new GameData.PlacedObjectData
                {
                    objectName = child.name,
                    position   = child.position,
                    rotation   = child.rotation,
                    localScale = child.localScale
                };
                save.objects.Add(pod);
            }
        }

        GameData.BuiltTrack = save;
        SetStatus("Track saved. Loading Race…");

        if (sceneLoader != null) sceneLoader.ChangeScene(raceSceneName);
        else UnityEngine.SceneManagement.SceneManager.LoadScene(raceSceneName);
    }

    void SetStatus(string msg)
    {
        if (statusText) statusText.text = msg;
        else Debug.Log(msg);
    }
}
