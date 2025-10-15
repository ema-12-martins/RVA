using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class TrackGenerator : MonoBehaviour
{
    [Header("Track Configuration")]
    [Tooltip("Control points defining the track's closed curve")]
    public Transform[] controlPoints;
    
    [Range(0.5f, 10f)]
    [Tooltip("Width of each lane")]
    public float laneWidth = 2f;
    
    [Range(0.1f, 2f)]
    [Tooltip("Thickness of the track")]
    public float trackThickness = 0.2f;
    
    [Range(10, 200)]
    [Tooltip("Number of segments per curve section")]
    public int segmentsPerCurve = 50;
    
    [Header("Materials")]
    public Material trackMaterial;
    public Material dividerMaterial;
    
    [Header("Generation")]
    public bool autoUpdate = true;
    
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private GameObject dividerObject;
    
    void OnValidate()
    {
        if (autoUpdate && Application.isPlaying == false)
        {
            // Delay generation to next frame to ensure all components are ready
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) // Check if object still exists
                    GenerateTrack();
            };
        }
    }
    
    void Start()
    {
        GenerateTrack();
    }
    
    [ContextMenu("Generate Track")]
    public void GenerateTrack()
    {
        if (controlPoints == null || controlPoints.Length < 3)
        {
            Debug.LogWarning("Need at least 3 control points to generate a track");
            return;
        }
        
        // Validate all control points exist
        foreach (var cp in controlPoints)
        {
            if (cp == null)
            {
                Debug.LogWarning("Some control points are null. Please assign all control points.");
                return;
            }
        }
        
        // Setup components
        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
            
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        
        // Generate curve points
        List<Vector3> curvePoints = GenerateCurvePoints();
        
        // Create track mesh
        Mesh trackMesh = CreateTrackMesh(curvePoints);
        
        if (meshFilter != null)
            meshFilter.sharedMesh = trackMesh;
        
        if (trackMaterial != null && meshRenderer != null)
            meshRenderer.sharedMaterial = trackMaterial;
        
        // Create divider line
        CreateDividerLine(curvePoints);
    }
    
    List<Vector3> GenerateCurvePoints()
    {
        List<Vector3> points = new List<Vector3>();
        int totalSegments = controlPoints.Length * segmentsPerCurve;
        
        for (int i = 0; i < totalSegments; i++)
        {
            float t = i / (float)totalSegments;
            Vector3 point = GetPointOnClosedCurve(t);
            points.Add(point);
        }
        
        return points;
    }
    
    Vector3 GetPointOnClosedCurve(float t)
    {
        int numPoints = controlPoints.Length;
        float scaledT = t * numPoints;
        int p0Index = Mathf.FloorToInt(scaledT);
        float localT = scaledT - p0Index;
        
        // Get 4 control points for Catmull-Rom spline
        int p0 = p0Index % numPoints;
        int p1 = (p0Index + 1) % numPoints;
        int p2 = (p0Index + 2) % numPoints;
        int p3 = (p0Index + 3) % numPoints;
        
        return CatmullRom(
            controlPoints[p0].position,
            controlPoints[p1].position,
            controlPoints[p2].position,
            controlPoints[p3].position,
            localT
        );
    }
    
    Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        
        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }
    
    Mesh CreateTrackMesh(List<Vector3> centerPoints)
    {
        Mesh mesh = new Mesh();
        mesh.name = "Racing Track";
        
        float totalWidth = laneWidth * 2;
        int pointCount = centerPoints.Count;
        
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        
        // Generate vertices along the curve
        for (int i = 0; i < pointCount; i++)
        {
            Vector3 current = centerPoints[i];
            Vector3 next = centerPoints[(i + 1) % pointCount];
            
            // Calculate perpendicular direction
            Vector3 forward = (next - current).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            
            // Create quad vertices (4 corners for thickness)
            Vector3 leftOuter = current - right * totalWidth / 2f;
            Vector3 rightOuter = current + right * totalWidth / 2f;
            
            // Top surface
            vertices.Add(leftOuter);
            vertices.Add(rightOuter);
            // Bottom surface
            vertices.Add(leftOuter - Vector3.up * trackThickness);
            vertices.Add(rightOuter - Vector3.up * trackThickness);
            
            // UVs
            float uvY = i / (float)pointCount;
            uvs.Add(new Vector2(0, uvY));
            uvs.Add(new Vector2(1, uvY));
            uvs.Add(new Vector2(0, uvY));
            uvs.Add(new Vector2(1, uvY));
        }
        
        // Generate triangles
        for (int i = 0; i < pointCount; i++)
        {
            int current = i * 4;
            int next = (i + 1) % pointCount * 4;
            
            // Top surface
            triangles.Add(current);
            triangles.Add(next);
            triangles.Add(current + 1);
            
            triangles.Add(current + 1);
            triangles.Add(next);
            triangles.Add(next + 1);
            
            // Bottom surface
            triangles.Add(current + 2);
            triangles.Add(current + 3);
            triangles.Add(next + 2);
            
            triangles.Add(current + 3);
            triangles.Add(next + 3);
            triangles.Add(next + 2);
            
            // Left side
            triangles.Add(current);
            triangles.Add(current + 2);
            triangles.Add(next);
            
            triangles.Add(next);
            triangles.Add(current + 2);
            triangles.Add(next + 2);
            
            // Right side
            triangles.Add(current + 1);
            triangles.Add(next + 1);
            triangles.Add(current + 3);
            
            triangles.Add(next + 1);
            triangles.Add(next + 3);
            triangles.Add(current + 3);
        }
        
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    
    void CreateDividerLine(List<Vector3> centerPoints)
    {
        // Create or get divider object
        if (dividerObject == null)
        {
            // Try to find existing divider first
            Transform existingDivider = transform.Find("Lane Divider");
            if (existingDivider != null)
            {
                dividerObject = existingDivider.gameObject;
            }
            else
            {
                dividerObject = new GameObject("Lane Divider");
                dividerObject.transform.parent = transform;
                dividerObject.transform.localPosition = Vector3.zero;
            }
        }
        
        MeshFilter dividerFilter = dividerObject.GetComponent<MeshFilter>();
        if (dividerFilter == null)
            dividerFilter = dividerObject.AddComponent<MeshFilter>();
            
        MeshRenderer dividerRenderer = dividerObject.GetComponent<MeshRenderer>();
        if (dividerRenderer == null)
            dividerRenderer = dividerObject.AddComponent<MeshRenderer>();
        
        Mesh dividerMesh = new Mesh();
        dividerMesh.name = "Lane Divider";
        
        float dividerWidth = 0.1f;
        float dividerHeight = 0.02f;
        
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        
        for (int i = 0; i < centerPoints.Count; i++)
        {
            Vector3 current = centerPoints[i];
            Vector3 next = centerPoints[(i + 1) % centerPoints.Count];
            
            Vector3 forward = (next - current).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            
            Vector3 leftPos = current - right * dividerWidth / 2f;
            Vector3 rightPos = current + right * dividerWidth / 2f;
            
            vertices.Add(leftPos + Vector3.up * dividerHeight);
            vertices.Add(rightPos + Vector3.up * dividerHeight);
        }
        
        for (int i = 0; i < centerPoints.Count; i++)
        {
            int current = i * 2;
            int next = (i + 1) % centerPoints.Count * 2;
            
            triangles.Add(current);
            triangles.Add(next);
            triangles.Add(current + 1);
            
            triangles.Add(current + 1);
            triangles.Add(next);
            triangles.Add(next + 1);
        }
        
        dividerMesh.vertices = vertices.ToArray();
        dividerMesh.triangles = triangles.ToArray();
        dividerMesh.RecalculateNormals();
        
        dividerFilter.sharedMesh = dividerMesh;
        
        if (dividerMaterial != null)
            dividerRenderer.sharedMaterial = dividerMaterial;
    }
    
    // Public method to get a point on the track at normalized position t (0 to 1)
    public Vector3 GetTrackPosition(float t)
    {
        if (controlPoints == null || controlPoints.Length < 3)
            return Vector3.zero;
        
        t = Mathf.Repeat(t, 1f);
        return GetPointOnClosedCurve(t);
    }
    
    // Get position offset to left or right for lanes
    public Vector3 GetLanePosition(float t, bool leftLane)
    {
        Vector3 centerPos = GetTrackPosition(t);
        float nextT = t + 0.001f;
        Vector3 nextPos = GetTrackPosition(nextT);
        
        Vector3 forward = (nextPos - centerPos).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        
        float offset = laneWidth / 2f;
        return centerPos + right * (leftLane ? -offset : offset);
    }
}