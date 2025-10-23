using UnityEngine;

[CreateAssetMenu(fileName = "BoosterEffect", menuName = "ScriptableObjects/ItemEffects/BoosterEffect")]
public class BoosterEffect : ItemEffect

{
    public float speedMultiplier = 2f;

    public override void ApplyEffect(GameObject target)
    {
        RacerAnimator racerAnimator = target.GetComponent<RacerAnimator>();
        if (racerAnimator != null)
        {
            racerAnimator.SetSpeed(racerAnimator.speed * speedMultiplier);
        }
    }

    public override void RemoveEffect(GameObject target)
    {
        RacerAnimator racerAnimator = target.GetComponent<RacerAnimator>();
        if (racerAnimator != null)
        {
            racerAnimator.SetSpeed(racerAnimator.speed / speedMultiplier);
        }
    }
}