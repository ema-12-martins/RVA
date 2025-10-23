using UnityEngine;
using UnityEngine.UI;

public abstract class ItemEffect : ScriptableObject
{
    public string itemName;
    public float duration;

    // Abstract method to apply the effect
    public abstract void ApplyEffect(GameObject target);

    // Optional method to remove the effect
    public virtual void RemoveEffect(GameObject target)
    {
        // Default implementation (can be overridden)
    }
}