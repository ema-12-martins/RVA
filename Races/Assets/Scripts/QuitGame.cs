using UnityEngine;
using UnityEngine.SceneManagement;

public class Quit : MonoBehaviour
{
    public void QuitGameFunction()
    {
        Debug.Log("Closing game...");
        Application.Quit();
    }

    public void ChangeScene(string sceneName)
    {
        Debug.Log("Loading next scene...");
        SceneManager.LoadScene(sceneName);
    }
}

