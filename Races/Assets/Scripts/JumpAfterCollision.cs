using UnityEngine;

public class JumpAfterCollision : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collision detected: " + other.name + "JUMP?");

        RacerAnimator racerAnimator = other.GetComponent<RacerAnimator>();

        if (racerAnimator != null)
        {
            if (UnityEngine.Random.value < GameData.probabilityOfOvercomingObstacles)
            {
                GameData.isJumpingForBot = true;
                GameData.jumpTimerForBot = 0;
            }
        }

    }
}

    
