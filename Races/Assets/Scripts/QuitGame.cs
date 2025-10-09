using UnityEngine;

public class QuitGame : MonoBehaviour
{
    public void QuitGameFunction()
    {
        Debug.Log("Closing game...");
        Application.Quit();
    }
}
