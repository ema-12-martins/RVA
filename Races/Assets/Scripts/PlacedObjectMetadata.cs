using UnityEngine;

public class PlacedObjectMetadata : MonoBehaviour
{
    [Tooltip("Normalized position along track curve [0-1]")]
    public float t;

    [Tooltip("True if placed on left lane, false if right lane")]
    public bool isLeftLane;

    [Tooltip("Rotation offset in degrees around vertical axis (typically 0, 90, 180, or 270)")]
    public float yawOffset;

    [Tooltip("Name/identifier of the prefab to spawn during reconstruction")]
    public string prefabName;
}