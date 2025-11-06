using UnityEngine;
using System.Collections;

[CreateAssetMenu(fileName = "StopEffect", menuName = "ScriptableObjects/ItemEffects/StopEffect")]
public class StopEffect : ItemEffect
{
    public float hideDuration = 1.5f;
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
            gameObject.SetActive(false);
            racerAnimator.StartCoroutine(ShowObjectCoroutine());
        }
    }

    IEnumerator ShowObjectCoroutine()
    {
        yield return new WaitForSeconds(hideDuration);
        gameObject.SetActive(true);
    }    
}
