using UnityEngine;

public class AutoPlaceStarterObjects : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The TrackGenerator component")]
    [SerializeField] TrackGenerator track;
    
    [Tooltip("Parent transform where placed objects should be stored")]
    [SerializeField] Transform placedObjectsParent;

    [Header("Prefabs")]
    [Tooltip("The tire pile prefab")]
    [SerializeField] GameObject tirePilePrefab;
    
    [Tooltip("The booster prefab")]
    [SerializeField] GameObject boosterPrefab;

    [Header("Settings")]
    [Tooltip("Minimum distance between auto-placed objects (normalized track position)")]
    [Range(0.1f, 0.5f)]
    [SerializeField] float minSpacing = 0.2f;

    [Tooltip("Exclusion zone at track start (normalized). No objects will spawn here.")]
    [Range(0f, 0.3f)]
    [SerializeField] float startExclusionZone = 0.15f;

    void Start()
    {
        // Wait one frame to ensure track is fully generated
        Invoke(nameof(PlaceStarterObjects), 0.1f);
    }

    void PlaceStarterObjects()
    {
        if (track == null)
        {
            Debug.LogError("TrackGenerator not assigned!");
            return;
        }

        if (tirePilePrefab == null || boosterPrefab == null)
        {
            Debug.LogError("Prefabs not assigned!");
            return;
        }

        // Ensure we have a parent for placed objects
        Transform parent = EnsurePlacedParent();

        // Generate 3 random positions with minimum spacing, avoiding start zone
        float[] positions = GenerateRandomPositions(3, minSpacing, startExclusionZone);

        // Sort positions to make spacing calculation easier
        System.Array.Sort(positions);

        // 1. Tire pile in right lane (player's lane), no rotation
        PlaceObject(tirePilePrefab, positions[0], false, 0f, parent);

        // 2. Booster in right lane (player's lane), rotated 180°
        PlaceObject(boosterPrefab, positions[1], false, 180f, parent);

        // 3. Booster in left lane, no rotation
        PlaceObject(boosterPrefab, positions[2], true, 0f, parent);

        Debug.Log($"Auto-placed 3 starter objects on track (avoiding first {startExclusionZone * 100}% of track)");
    }

    float[] GenerateRandomPositions(int count, float minSpacing, float exclusionStart)
    {
        float[] positions = new float[count];
        
        // Valid range starts after exclusion zone
        float minT = exclusionStart;
        float maxT = 0.9f;
        
        for (int i = 0; i < count; i++)
        {
            bool validPosition = false;
            int attempts = 0;
            
            while (!validPosition && attempts < 100)
            {
                // Generate random position in valid range
                float candidate = Random.Range(minT, maxT);
                
                // Check if it's far enough from all previous positions
                validPosition = true;
                for (int j = 0; j < i; j++)
                {
                    float distance = Mathf.Abs(candidate - positions[j]);
                    
                    // Handle wrap-around for closed tracks
                    if (track.isClosed)
                    {
                        distance = Mathf.Min(distance, 1f - distance);
                    }
                    
                    if (distance < minSpacing)
                    {
                        validPosition = false;
                        break;
                    }
                }
                
                if (validPosition)
                {
                    positions[i] = candidate;
                }
                
                attempts++;
            }
            
            // Fallback: if we couldn't find a valid position, space them evenly in valid range
            if (attempts >= 100)
            {
                float validRange = maxT - minT;
                positions[i] = minT + validRange * (i + 1) / (float)(count + 1);
            }
        }
        
        return positions;
    }

    void PlaceObject(GameObject prefab, float t, bool isLeftLane, float yawOffset, Transform parent)
    {
        // Get lane position
        Vector3 position = track.GetLanePosition(t, isLeftLane);
        
        // Calculate rotation based on track direction
        const float dt = 1f / 2048f;
        Vector3 p0 = track.GetTrackPosition(t);
        Vector3 p1 = track.GetTrackPosition(t + dt);
        Vector3 forward = (p1 - p0).sqrMagnitude > 1e-8f ? (p1 - p0).normalized : Vector3.forward;
        
        Quaternion trackRotation = Quaternion.LookRotation(forward, Vector3.up);
        Quaternion finalRotation = trackRotation * Quaternion.Euler(0f, yawOffset, 0f);
        
        // Instantiate the object
        GameObject placed = Instantiate(prefab, position, finalRotation, parent);
        placed.name = prefab.name + "_AutoPlaced";
        
        // Add metadata so it can be saved/loaded like manually placed objects
        var meta = placed.AddComponent<PlacedObjectMetadata>();
        meta.t = t;
        meta.isLeftLane = isLeftLane;
        meta.yawOffset = yawOffset;
        meta.prefabName = prefab.name;
    }

    Transform EnsurePlacedParent()
    {
        if (placedObjectsParent != null)
            return placedObjectsParent;
        
        // Try to find existing parent
        var existing = track.transform.Find("PlacedObjects");
        if (existing != null)
            return existing;
        
        // Create new parent
        var go = new GameObject("PlacedObjects");
        go.transform.SetParent(track.transform, false);
        return go.transform;
    }
}