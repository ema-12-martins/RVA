using UnityEngine;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public enum GameState { WaitingForTrack, Countdown, Racing, Paused, TargetLost, Finished }

    [Header("Change Scenes")]
    public SceneLoader sceneLoader;

    [Header("AR Setup")]
    public TrackTargetHandler trackTargetHandler;

    [Header("Race Setup")]
    public TrackGenerator track;
    public TrackGenerator shortcut;
    public int lapsToWin = 2;

    [Header("Prefabs")]
    public GameObject opponentCarPrefab;

    [Header("Placed Objects Reconstruction")]
    [Tooltip("Array of prefabs that can be placed on the track (must match names from planning scene)")]
    public GameObject[] placeableObjectPrefabs;
    [Tooltip("Parent transform where reconstructed objects will be placed (leave null to auto-create)")]
    public Transform placedObjectsParent;

    [Header("UI")]
    public TextMeshProUGUI countdownText;
    public GameObject targetLostPanel;
    public TextMeshProUGUI targetLostText;

    private RacerAnimator playerRacer;
    private RacerAnimator opponentRacer;
    private GameObject playerCarInstance;
    private GameObject opponentCarInstance;

    private int playerLaps = 0;
    private int opponentLaps = 0;

    private GameState currentState = GameState.WaitingForTrack;
    private bool racersSpawned = false;
    private bool objectsReconstructed = false;
    private Coroutine countdownCoroutine;

    public static float selected_track = 1f;

    public string winnerText = null;
    public string winnerMsg = "";

    void Start()
    {
        Time.timeScale = 1f;
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (targetLostPanel != null) targetLostPanel.SetActive(false);

        if (track == null) Debug.LogError("TrackGenerator not assigned to GameManager!");
        if (GameData.selectedCarPrefab == null)
        {
             Debug.LogError("No car selected! Returning to Start Menu.");
             sceneLoader.ChangeScene("StartMenu");
             return;
        }
        if (opponentCarPrefab == null) Debug.LogError("OpponentCarPrefab not assigned!");
        if (countdownText == null) Debug.LogError("Countdown Text not assigned!");
        if (targetLostPanel == null || targetLostText == null) Debug.LogError("Target Lost UI elements not assigned!");
        if (trackTargetHandler == null)
        {
             Debug.LogError("TrackTargetHandler not assigned! AR detection won't work.");
        }
        else
        {
            trackTargetHandler.OnTrackFound += HandleTrackFound;
            trackTargetHandler.OnTrackLost += HandleTrackLost;
        }

        ReconstructPlacedObjects();

        ChangeState(GameState.WaitingForTrack);

        // Spawn Racers (keep them inactive/non-moving initially)
        SpawnRacers();
        SetRacersActive(false);
    }

    void ReconstructPlacedObjects()
    {
        if (objectsReconstructed)
        {
            Debug.LogWarning("Objects already reconstructed!");
            return;
        }

        // Check if we have saved track data
        if (GameData.BuiltTrack == null || GameData.BuiltTrack.objects == null || GameData.BuiltTrack.objects.Count == 0)
        {
            Debug.Log("No placed objects to reconstruct.");
            objectsReconstructed = true;
            return;
        }

        if (placedObjectsParent == null)
        {
            placedObjectsParent = track.transform.Find("PlacedObjects");
            if (placedObjectsParent == null)
            {
                var go = new GameObject("PlacedObjects");
                go.transform.SetParent(track.transform, false);
                placedObjectsParent = go.transform;
            }
        }

        int reconstructedCount = 0;
        foreach (var objData in GameData.BuiltTrack.objects)
        {
            // Find matching prefab by name
            GameObject prefab = System.Array.Find(placeableObjectPrefabs, p => p.name == objData.prefabName);
            
            if (prefab == null)
            {
                Debug.LogWarning($"Prefab '{objData.prefabName}' not found in placeableObjectPrefabs array. Skipping.");
                continue;
            }

            // Calculate position from parametric data
            Vector3 position = track.GetLanePosition(objData.t, objData.isLeftLane);

            // Calculate rotation from track tangent + yaw offset
            float dt = 0.001f;
            Vector3 p0 = track.GetTrackPosition(objData.t);
            Vector3 p1 = track.GetTrackPosition(objData.t + dt);
            Vector3 forward = (p1 - p0).sqrMagnitude > 1e-8f ? (p1 - p0).normalized : Vector3.forward;
            Quaternion laneRotation = Quaternion.LookRotation(forward, Vector3.up);
            Quaternion finalRotation = laneRotation * Quaternion.Euler(0f, objData.yawOffset, 0f);

            // Instantiate the object
            GameObject reconstructedObj = Instantiate(prefab, position, finalRotation, placedObjectsParent);
            reconstructedObj.name = objData.prefabName + "_Reconstructed";

            reconstructedCount++;
        }

        Debug.Log($"Reconstructed {reconstructedCount} placed object(s) from GameData.");
        objectsReconstructed = true;
    }

    void SpawnRacers()
    {
        if (racersSpawned) return; // Only spawn once

        if (GameData.selectedCarPrefab != null)
        {
            playerCarInstance = Instantiate(GameData.selectedCarPrefab, track.transform.position, track.transform.rotation);
            playerCarInstance.transform.SetParent(track.transform, true);
            playerRacer = playerCarInstance.GetComponent<RacerAnimator>();
            if (playerRacer != null)
            {
                playerRacer.track = track;
                playerRacer.shortcut = shortcut;
                playerRacer.isPlayer = true;
                playerRacer.leftLane = false;
                playerRacer.isPlayerControlled = true;
                playerRacer.OnLapCompleted += HandlePlayerLap;
                playerRacer.name = "PlayerCar";
                playerRacer.InitializeRacer();
                playerRacer.enabled = false;
            }
            else { Debug.LogError("Selected Car Prefab does not have a RacerAnimator component!"); }
        }

        if (opponentCarPrefab != null)
        {
            opponentCarInstance = Instantiate(opponentCarPrefab, track.transform.position, track.transform.rotation);
            opponentCarInstance.transform.SetParent(track.transform, true);
            opponentRacer = opponentCarInstance.GetComponent<RacerAnimator>();
            if (opponentRacer != null)
            {
                opponentRacer.track = track;
                opponentRacer.shortcut = shortcut;
                opponentRacer.isPlayer = false;
                opponentRacer.leftLane = true;
                opponentRacer.isPlayerControlled = false;
                opponentRacer.OnLapCompleted += HandleOpponentLap;
                opponentRacer.name = "OpponentCar";
                opponentRacer.InitializeRacer();
                 opponentRacer.enabled = false;
            }
            else { Debug.LogError("Opponent Car Prefab does not have a RacerAnimator component!"); }
        }
        racersSpawned = true;
    }

    void ChangeState(GameState newState)
    {
        if (currentState == newState) return;

        Debug.Log($"Changing State from {currentState} to {newState}");
        currentState = newState;

        switch (currentState)
        {
            case GameState.WaitingForTrack:
                Time.timeScale = 1f;
                SetRacersActive(false);
                if (countdownText != null) countdownText.gameObject.SetActive(false);
                if (targetLostPanel != null) targetLostPanel.SetActive(false);
                break;
            case GameState.Countdown:
                if(countdownCoroutine != null) StopCoroutine(countdownCoroutine);
                ResetRaceState();
                SetRacersActive(true); // Activate racers visually, but scripts still disabled
                if(playerRacer) playerRacer.enabled = false;
                if(opponentRacer) opponentRacer.enabled = false;
                countdownCoroutine = StartCoroutine(CountdownCoroutine());
                break;
            case GameState.Racing:
                Time.timeScale = 1f;
                SetRacersActive(true); // Ensure racers are active and scripts enabled
                 if(playerRacer) playerRacer.enabled = true;
                 if(opponentRacer) opponentRacer.enabled = true;
                if (countdownText != null) countdownText.gameObject.SetActive(false);
                if (targetLostPanel != null) targetLostPanel.SetActive(false);
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
             case GameState.TargetLost:
                Time.timeScale = 0f;
                ShowTargetLostMessage();
                 // Keep racers visually active but disable their scripts
                 if(playerRacer) playerRacer.enabled = false;
                 if(opponentRacer) opponentRacer.enabled = false;
                break;
            case GameState.Finished:
                Time.timeScale = 1f;
                // Ensure racers stop moving immediately
                 if(playerRacer) playerRacer.enabled = false;
                 if(opponentRacer) opponentRacer.enabled = false;
                break;
        }
    }

     // Called by TrackTargetHandler when the track image is found
    public void HandleTrackFound()
    {
        Debug.Log("Track Found");
        if (currentState == GameState.WaitingForTrack)
        {
            ChangeState(GameState.Countdown);
        }
        else if (currentState == GameState.TargetLost)
        {
             ChangeState(GameState.Racing); // Resuming re-enables scripts and set timescale
        }
    }

     // Called by TrackTargetHandler when the track image is lost
    public void HandleTrackLost()
    {
         Debug.Log("Track Lost");
         // Only pause if we were actively racing or in countdown
        if (currentState == GameState.Racing || currentState == GameState.Countdown)
        {
             if(countdownCoroutine != null)
             {
                 StopCoroutine(countdownCoroutine); // Stop countdown if lost during it
                 countdownCoroutine = null;
                 if (countdownText != null) countdownText.gameObject.SetActive(false);
             }
             ChangeState(GameState.TargetLost);
        }
    }

    IEnumerator CountdownCoroutine()
    {
        if (countdownText == null) yield break;

        countdownText.gameObject.SetActive(true);
        countdownText.text = "3";
        yield return new WaitForSeconds(1f);

         if(currentState != GameState.Countdown) yield break;

        countdownText.text = "2";
        yield return new WaitForSeconds(1f);

         if(currentState != GameState.Countdown) yield break;

        countdownText.text = "1";
        yield return new WaitForSeconds(1f);

         if(currentState != GameState.Countdown) yield break;

        countdownText.text = "GO!";
        ChangeState(GameState.Racing);

        yield return new WaitForSeconds(0.5f);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        countdownCoroutine = null;
    }

    void SetRacersActive(bool isActive)
    {
        if (playerCarInstance != null)
        {
             playerCarInstance.SetActive(isActive);
        }
        if (opponentCarInstance != null)
        {
             opponentCarInstance.SetActive(isActive);
        }
    }

    void HandlePlayerLap()
    {
        if (currentState != GameState.Racing) return;
        playerLaps++;
        Debug.Log($"Player completed lap {playerLaps}");
        CheckWinCondition();
    }

    void HandleOpponentLap()
    {
        if (currentState != GameState.Racing) return;
        opponentLaps++;
        Debug.Log($"Opponent completed lap {opponentLaps}");
        CheckWinCondition();
    }

     void ResetRaceState()
     {
        playerLaps = 0;
        opponentLaps = 0;
        if (playerRacer != null) playerRacer.ResetPosition();
        if (opponentRacer != null) opponentRacer.ResetPosition();
         Debug.Log("Race state reset.");
     }

    void CheckWinCondition()
    {
        if (currentState != GameState.Racing) return; // Only check if racing

        bool playerWins = playerLaps >= lapsToWin;
        bool opponentWins = opponentLaps >= lapsToWin;


        if (playerWins && opponentWins) // Tie condition
        {
             winnerMsg = "It's a Tie!";
        }
        else if (playerWins)
        {
             winnerMsg = "Player Wins!";
        }
        else if (opponentWins)
        {
            string winnerName = opponentRacer != null ? opponentRacer.gameObject.name : "Opponent";
            if(winnerName.Contains("(Clone)")) winnerName = winnerName.Replace("(Clone)", "").Trim();
             winnerMsg = $"{winnerName} Wins!";
        }

        if(!string.IsNullOrEmpty(winnerMsg))
        {
            EndRace(winnerMsg);
        }
    }

    void EndRace(string message)
    {
        if (currentState == GameState.Finished) return; // Don't end twice

        ChangeState(GameState.Finished);
        Debug.Log($"Race Finished: {message}");

        GameData.finalText = winnerMsg;

        sceneLoader.ChangeScene("FinalMenu");
    }

    void ShowTargetLostMessage()
    {
        if (targetLostPanel != null)
        {
            if(targetLostText != null) targetLostText.text = "Track target lost!\nPoint the camera back at the target to resume, or return to Main Menu.";
            targetLostPanel.SetActive(true);
        }
        if (countdownText != null) countdownText.gameObject.SetActive(false);
    }


    void OnDestroy()
    {
        if(playerRacer != null) playerRacer.OnLapCompleted -= HandlePlayerLap;
        if(opponentRacer != null) opponentRacer.OnLapCompleted -= HandleOpponentLap;
        if (trackTargetHandler != null)
        {
            trackTargetHandler.OnTrackFound -= HandleTrackFound;
            trackTargetHandler.OnTrackLost -= HandleTrackLost;
        }

         if(countdownCoroutine != null) StopCoroutine(countdownCoroutine);
    }
}