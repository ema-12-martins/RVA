using UnityEngine;

public abstract class ItemEffect : ScriptableObject
{
    public string itemName;
    public float duration;

    public GameObject gameObject;

    public int dir = 0;

    public bool useDirection = false;

    public abstract void ApplyEffect(GameObject target);

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