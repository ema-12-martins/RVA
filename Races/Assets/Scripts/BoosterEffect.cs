using System.Numerics;
using Unity.VisualScripting;
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

    public override void ApplyEffect(GameObject target, int d = 1)
    {
        this.dir = d;
        if (d < 0 && useDirection)
        {
            RemoveEffect(target);
        }
        else
        {
            ApplyEffect(target);
        }
    }

    public override void RemoveEffect(GameObject target, int d = 1)
    {
        if (this.dir < 0 && useDirection)
        {
            ApplyEffect(target);
        }else
        {
            RemoveEffect(target);
        }
    }
}