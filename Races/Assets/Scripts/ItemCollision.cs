using UnityEngine;
using UnityEngine.Animations;

public class ItemCollision : MonoBehaviour
{
    public ItemEffect itemEffect;

    void Start()
    {
        if (itemEffect is StopEffect)
        {
            itemEffect.gameObject = gameObject;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("ItemCollision detected with: " + other.name);
        RacerAnimator racerAnimator = other.GetComponent<RacerAnimator>();
        if (racerAnimator != null)
        {
            if (itemEffect.useDirection)
            {
                Vector3 carForward = other.transform.forward;
                Vector3 itemForward = transform.parent.forward;

                Debug.Log("Car Forward: " + carForward);
                Debug.Log("Item Forward: " + itemForward);

                float directionDot = Vector3.Dot(carForward, itemForward);
                itemEffect.dir = directionDot >= 0 ? 1 : -1;

                if (directionDot >= 0)
                {
                    Debug.Log("Item approached from the front.");
                }
                else
                {
                    Debug.Log("Item approached from behind.");
                }
            }

            racerAnimator.AddEffect(itemEffect);
        }
    }
}