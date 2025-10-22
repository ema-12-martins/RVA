// GameManager.cs
using UnityEngine;
using UnityEngine.SceneManagement; // Needed for scene loading
using TMPro; // Needed for UI text

public class GameManager : MonoBehaviour
{
    [Header("Race Setup")]
    public TrackGenerator track;
    public int lapsToWin = 1; // Set how many laps are needed

    [Header("Prefabs")]
    public GameObject opponentCarPrefab; // Assign opponent prefab in Inspector

    [Header("Spawn Points")]
    // Optional: Use transforms for precise start positions/rotations if needed
    // public Transform playerStartPoint;
    // public Transform opponentStartPoint;

    [Header("UI")]
    public GameObject endGamePanel; // Assign the end game panel UI object
    public TextMeshProUGUI winnerText; // Assign the text element for the winner message

    private RacerAnimator playerRacer;
    private RacerAnimator opponentRacer;

    private int playerLaps = 0;
    private int opponentLaps = 0;
    private bool raceFinished = false;

    void Start()
    {
        // --- ADD THIS LOG AT THE VERY TOP ---
        Debug.Log($"GameManager Start(). GameData prefab is: {(GameData.selectedCarPrefab == null ? "NULL" : GameData.selectedCarPrefab.name)}");
        // --- END ADD ---
        
        // Ensure UI is hidden at start
        if (endGamePanel != null) endGamePanel.SetActive(false);

        // Validate references
        if (track == null) Debug.LogError("TrackGenerator not assigned to GameManager!");
        if (GameData.selectedCarPrefab == null) Debug.LogError("No car selected from previous scene!");
        if (opponentCarPrefab == null) Debug.LogError("OpponentCarPrefab not assigned!");
        if (endGamePanel == null || winnerText == null) Debug.LogError("End game UI elements not assigned!");

        SpawnRacers();
    }

    void SpawnRacers()
    {
        if (GameData.selectedCarPrefab != null)
        {
            // Instantiate Player Car (e.g., in the left lane AT the origin, GameManager handles positioning logic now via InitializeRacer)
            GameObject playerCar = Instantiate(GameData.selectedCarPrefab, Vector3.zero, Quaternion.identity); // Instantiate at world origin first
            playerRacer = playerCar.GetComponent<RacerAnimator>();
            if (playerRacer != null)
            {
                playerRacer.track = track; // Assign track
                playerRacer.leftLane = true;
                playerRacer.isPlayerControlled = true;
                playerRacer.OnLapCompleted += HandlePlayerLap;
                playerRacer.name = "PlayerCar";
                playerRacer.InitializeRacer(); // <<< ADD THIS CALL
            }
             else { Debug.LogError("Selected Car Prefab does not have a RacerAnimator component!"); }
        }

        if (opponentCarPrefab != null)
        {
             // Instantiate Opponent Car (e.g., in the right lane AT the origin)
            GameObject opponentCar = Instantiate(opponentCarPrefab, Vector3.zero, Quaternion.identity); // Instantiate at world origin first
            opponentRacer = opponentCar.GetComponent<RacerAnimator>();
            if (opponentRacer != null)
            {
                opponentRacer.track = track; // Assign track
                opponentRacer.leftLane = false;
                opponentRacer.isPlayerControlled = false;
                opponentRacer.OnLapCompleted += HandleOpponentLap;
                 opponentRacer.name = "OpponentCar";
                 opponentRacer.speed = 0.95f;
                 opponentRacer.InitializeRacer(); // <<< ADD THIS CALL
            }
             else { Debug.LogError("Opponent Car Prefab does not have a RacerAnimator component!"); }
        }
    }

    void HandlePlayerLap()
    {
        if (raceFinished) return;
        playerLaps++;
        Debug.Log($"Player completed lap {playerLaps}");
        CheckWinCondition();
    }

    void HandleOpponentLap()
    {
        if (raceFinished) return;
        opponentLaps++;
        Debug.Log($"Opponent completed lap {opponentLaps}");
        CheckWinCondition();
    }

    void CheckWinCondition()
    {
        bool playerWins = playerLaps >= lapsToWin;
        bool opponentWins = opponentLaps >= lapsToWin;

        if (playerWins && opponentWins)
        {
            EndRace("It's a Tie!");
        }
        else if (playerWins)
        {
            EndRace("Player Wins!");
        }
        else if (opponentWins)
        {
            // Determine winner name based on prefab if possible
            string winnerName = opponentRacer != null ? opponentRacer.gameObject.name : "Opponent";
             if(winnerName.Contains("(Clone)")) winnerName = winnerName.Replace("(Clone)", "").Trim(); // Clean up name
            EndRace($"{winnerName} Wins!");
        }
    }

    void EndRace(string message)
    {
        raceFinished = true;
        Debug.Log($"Race Finished: {message}");

        // Stop racers (optional, could just let them finish the current animation)
        // if (playerRacer != null) playerRacer.enabled = false;
        // if (opponentRacer != null) opponentRacer.enabled = false;

        // Show End Game UI
        if (winnerText != null) winnerText.text = message;
        if (endGamePanel != null) endGamePanel.SetActive(true);
    }

     // --- Public functions to be called by UI Buttons ---
    public void GoToMainMenu()
    {
        SceneLoader.Instance.LoadMainMenu();
    }

     public void QuitGame()
    {
        SceneLoader.Instance.QuitGameFunction();
    }

     // Clean up event subscriptions
    void OnDestroy()
    {
        if(playerRacer != null) playerRacer.OnLapCompleted -= HandlePlayerLap;
        if(opponentRacer != null) opponentRacer.OnLapCompleted -= HandleOpponentLap;
    }
}