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
            // Instantiate Player Car (e.g., in the left lane)
            GameObject playerCar = Instantiate(GameData.selectedCarPrefab, track.GetLanePosition(0f, true) + Vector3.up * 0.3f, Quaternion.identity);
            playerRacer = playerCar.GetComponent<RacerAnimator>();
            if (playerRacer != null)
            {
                playerRacer.track = track;
                playerRacer.leftLane = true; // Player in left lane
                playerRacer.isPlayerControlled = true; // Add this flag to RacerAnimator
                playerRacer.OnLapCompleted += HandlePlayerLap; // Subscribe to lap event
                playerRacer.name = "PlayerCar"; // Rename for clarity
            }
             else { Debug.LogError("Selected Car Prefab does not have a RacerAnimator component!"); }
        }

        if (opponentCarPrefab != null)
        {
             // Instantiate Opponent Car (e.g., in the right lane)
            GameObject opponentCar = Instantiate(opponentCarPrefab, track.GetLanePosition(0f, false) + Vector3.up * 0.3f, Quaternion.identity);
            opponentRacer = opponentCar.GetComponent<RacerAnimator>();
            if (opponentRacer != null)
            {
                opponentRacer.track = track;
                opponentRacer.leftLane = false; // Opponent in right lane
                opponentRacer.isPlayerControlled = false; // Opponent is AI/Simple Mover
                opponentRacer.OnLapCompleted += HandleOpponentLap; // Subscribe to lap event
                 opponentRacer.name = "OpponentCar"; // Rename for clarity
                // Optional: Adjust opponent speed slightly
                 opponentRacer.speed = 0.95f;
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