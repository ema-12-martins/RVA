using UnityEngine;

public class JumpAfterCollision : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Collision detected: " + other.name + "JUMP?");

        RacerAnimator racerAnimator = other.GetComponent<RacerAnimator>();

        if (racerAnimator != null)
        {
            Debug.Log("It's a racer! Deciding to jump...");
            if (UnityEngine.Random.value < GameData.probabilityOfOvercomingObstacles)
            {
                Debug.Log("Racer will jump!");
                GameData.isJumpingForBot = true;
            }
        }

    }
}

    
