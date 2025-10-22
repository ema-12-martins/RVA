using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // --- Singleton Implementation ---
    private static SceneLoader _instance; // Private reference to the single instance

    public static SceneLoader Instance // Public static property to access the instance
    {
        get
        {
            // If the instance hasn't been found yet
            if (_instance == null)
            {
                // Try to find it in the scene
                _instance = FindAnyObjectByType<SceneLoader>();

                // If it's still not found, create a new GameObject and add the script
                if (_instance == null)
                {
                    GameObject singletonObject = new GameObject("SceneLoaderSingleton");
                    _instance = singletonObject.AddComponent<SceneLoader>();
                    Debug.Log("SceneLoader Singleton created.");
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        // Ensure there's only one instance
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject); // Destroy duplicate
            return;
        }
        _instance = this; // Set the instance
        DontDestroyOnLoad(gameObject);
    }
    // --- End Singleton Implementation ---

    // --- Scene Management Functions ---
    public void QuitGameFunction()
    {
        Debug.Log("Closing game...");
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Also stop play mode in editor
#endif
    }

    public void ChangeScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name cannot be empty!");
            return;
        }
        Debug.Log($"Loading scene: {sceneName}...");
        SceneManager.LoadScene(sceneName);
    }

    public void LoadMainMenu() // Convenience function
    {
        ChangeScene("StartMenu");
    }
}