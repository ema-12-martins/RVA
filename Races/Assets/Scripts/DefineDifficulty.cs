using UnityEngine;
using UnityEngine.SceneManagement;

public class DefineDifficulty : MonoBehaviour
{
    public void SetDifficulty(int level)
    {
        if (level == 1)
        {
            GameData.probabilityOfOvercomingObstacles = 0.3f;
        }else if (level == 2)
        {
            GameData.probabilityOfOvercomingObstacles = 0.6f;
        }
        else if (level == 3)
        {
            GameData.probabilityOfOvercomingObstacles = 0.9f;
        }
    }

}
