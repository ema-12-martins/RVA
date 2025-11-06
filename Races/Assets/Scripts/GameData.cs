using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameData
{
    public static GameObject selectedCarPrefab = null;
    public static string finalText = null; // If win or lost
    public static float probabilityOfOvercomingObstacles = 0;

    [Serializable]
    public class PlacedObjectData
    {
        public string prefabName;
        public float t; // Normalized position along track [0-1]
        public bool isLeftLane;
        public float yawOffset; // Rotation offset in degrees
    }

    [Serializable]
    public class TrackSaveData
    {        
        public List<PlacedObjectData> objects;
    }

    public static TrackSaveData BuiltTrack = null;
}