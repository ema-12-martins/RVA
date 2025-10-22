using UnityEngine;

public class StartMenuManager : MonoBehaviour
{
    public void StartGame()
    {
        // Load the car selection scene
        SceneLoader.Instance.ChangeScene("SelectCar");
    }
}