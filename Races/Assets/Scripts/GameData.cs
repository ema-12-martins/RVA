// GameData.cs (Make sure it looks like this)
using UnityEngine;

public static class GameData
{
    private static GameObject _selectedCarPrefab = null;

    public static GameObject selectedCarPrefab
    {
        get { return _selectedCarPrefab; }
        set
        {
            // --- THIS LOG IS KEY ---
            Debug.Log($"GameData.selectedCarPrefab SET TO: {(_selectedCarPrefab == null ? "NULL" : _selectedCarPrefab.name)} // New value: {(value == null ? "NULL" : value.name)}");
            // UnityEngine.Debug.Log(System.Environment.StackTrace); // Optional: Uncomment for detailed call stack
            _selectedCarPrefab = value;
            // --- END LOG ---
        }
    }
}