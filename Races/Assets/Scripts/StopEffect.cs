using UnityEngine;

[CreateAssetMenu(fileName = "StopEffect", menuName = "ScriptableObjects/ItemEffects/StopEffect")]
public class StopEffect : ItemEffect
{
    public float stopDuration = 3f;

    public override void ApplyEffect(GameObject target)
    {
        RacerAnimator racerAnimator = target.GetComponent<RacerAnimator>();
        if (racerAnimator != null)
        {
            racerAnimator.SetSpeed(0f);
        }
    }

    public override void RemoveEffect(GameObject target)
    {
        RacerAnimator racerAnimator = target.GetComponent<RacerAnimator>();
        if (racerAnimator != null)
        {
            racerAnimator.SetSpeed(1f);
        }
    }
}
