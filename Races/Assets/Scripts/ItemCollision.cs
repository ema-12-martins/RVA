using UnityEngine;

public class ItemCollision : MonoBehaviour
{
    public ItemEffect itemEffect;
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("ItemCollision detected with: " + other.name);
        RacerAnimator racerAnimator = other.GetComponent<RacerAnimator>();
        if (racerAnimator != null)
        {
            // Apply item effect to the racer
            racerAnimator.AddEffect(itemEffect);
        }
    }
}