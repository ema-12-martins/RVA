using UnityEngine;
using UnityEngine.UI;

public abstract class ItemEffect : ScriptableObject
{
    public string itemName;
    public float duration;

    public int dir = 0;

    public bool useDirection = false;

    // Abstract method to apply the effect
    public abstract void ApplyEffect(GameObject target);

    // Optional method to remove the effect
    public virtual void RemoveEffect(GameObject target)
    {
        // Default implementation (can be overridden)
    }

    public virtual void ApplyEffect(GameObject target, int dir = 1)
    {
        ApplyEffect(target);
    }

    public virtual void RemoveEffect(GameObject target, int dir = 1)
    {
        RemoveEffect(target);
    }

}