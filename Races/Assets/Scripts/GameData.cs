using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameData
{
    // Existing fields
    public static GameObject selectedCarPrefab = null;
    public static string finalText = null; // If win or lost
    public static float probabilityOfOvercomingObstacles = 0;

    // Modified: Parametric placement data instead of world transforms
    [Serializable]
    public class PlacedObjectData
    {
        public string prefabName;        // Name/identifier of the prefab
        public float t;                  // Normalized position along track [0-1]
        public bool isLeftLane;          // True = left lane, false = right lane
        public float yawOffset;          // Rotation offset in degrees (typically 0 or 180)
    }

    [Serializable]
    public class TrackSaveData
    {        
        public List<PlacedObjectData> objects;   // Parametric placement data
    }

    public static TrackSaveData BuiltTrack = null;
}