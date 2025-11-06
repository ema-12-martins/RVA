using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameData
{
    // Existing fields
    public static GameObject selectedCarPrefab = null;
    public static string finalText = null; // If win or lost
    public static float probabilityOfOvercomingObstacles = 0;

    // Track save payload
    [Serializable]
    public class PlacedObjectData
    {
        public string objectName;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 localScale;
    }

    [Serializable]
    public class TrackSaveData
    {
        public Vector3[] controlPoints;          // from TrackGenerator
        public List<PlacedObjectData> objects;   // baked objects under PlacedObjects
    }

    public static TrackSaveData BuiltTrack = null;
}
