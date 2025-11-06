using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class TrackGenerator : MonoBehaviour
{
    [Header("Track Configuration")]
    [Tooltip("Control points defining the track's curve")]
    public Transform[] controlPoints;

    [Header("Topology")]
    [Tooltip("If true, the curve wraps around; if false, it is an open curve")]
    public bool isClosed = true;

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
    private float cachedTrackLength = -1f;

    void OnValidate()
    {
        if (autoUpdate && Application.isPlaying == false) {}
        cachedTrackLength = -1f;
    }

    void Start()
    {
        GenerateTrack();
    }

    [ContextMenu("Generate Track")]
    public void GenerateTrack()
    {
        if (controlPoints == null || controlPoints.Length < 2)
        {
            Debug.LogWarning("Need at least 2 control points to generate a track");
            return;
        }

        foreach (var cp in controlPoints)
        {
            if (cp == null)
            {
                Debug.LogWarning("Some control points are null. Please assign all control points.");
                return;
            }
        }

        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        List<Vector3> curvePoints = GenerateCurvePoints();

        cachedTrackLength = CalculateTrackLength(curvePoints);

        Mesh trackMesh = CreateTrackMesh(curvePoints);

        if (meshFilter != null)
            meshFilter.sharedMesh = trackMesh;

        if (trackMaterial != null && meshRenderer != null)
            meshRenderer.sharedMaterial = trackMaterial;

        CreateDividerLine(curvePoints);
    }

    List<Vector3> GenerateCurvePoints()
    {
        List<Vector3> points = new List<Vector3>();
        int baseSegments = isClosed ? controlPoints.Length : (controlPoints.Length - 1);
        int totalSegments = Mathf.Max(1, baseSegments) * segmentsPerCurve;

        // Ensure at least 2 samples to avoid division by zero
        int samples = Mathf.Max(2, totalSegments);

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)(samples - 1);
            Vector3 point = GetPointOnCurve(t);
            points.Add(point);
        }

        return points;
    }

    float CalculateTrackLength(List<Vector3> curvePoints)
    {
        if (curvePoints == null || curvePoints.Count < 2) return 0f;

        float length = 0f;
        for (int i = 0; i < curvePoints.Count - 1; i++)
        {
            Vector3 current = curvePoints[i];
            Vector3 next = curvePoints[i + 1];
            length += Vector3.Distance(current, next);
        }

        if (isClosed && curvePoints.Count > 1)
        {
            // Close the loop
            length += Vector3.Distance(curvePoints[curvePoints.Count - 1], curvePoints[0]);
        }
        return length;
    }

    // Get the physical length of the track
    public float GetTrackLength()
    {
        if (cachedTrackLength < 0f)
        {
            List<Vector3> curvePoints = GenerateCurvePoints();
            cachedTrackLength = CalculateTrackLength(curvePoints);
        }
        return cachedTrackLength;
    }

    // Unified curve sampler that handles open/closed
    Vector3 GetPointOnCurve(float t)
    {
        if (controlPoints == null || controlPoints.Length < 2)
            return Vector3.zero;

        if (isClosed)
        {
            int num = controlPoints.Length;
            float scaledT = Mathf.Repeat(t, 1f) * num;
            int p0Index = Mathf.FloorToInt(scaledT);
            float localT = scaledT - p0Index;

            int p0 = p0Index % num;
            int p1 = (p0Index + 1) % num;
            int p2 = (p0Index + 2) % num;
            int p3 = (p0Index + 3) % num;

            return CatmullRom(
                controlPoints[p0].position,
                controlPoints[p1].position,
                controlPoints[p2].position,
                controlPoints[p3].position,
                localT
            );
        }
        else
        {
            int numSegs = controlPoints.Length - 1;
            float scaledT = Mathf.Clamp01(t) * numSegs;
            int seg = Mathf.FloorToInt(scaledT);
            float localT = Mathf.Clamp01(scaledT - seg);

            int i0 = Mathf.Clamp(seg - 1, 0, controlPoints.Length - 1);
            int i1 = Mathf.Clamp(seg,     0, controlPoints.Length - 1);
            int i2 = Mathf.Clamp(seg + 1, 0, controlPoints.Length - 1);
            int i3 = Mathf.Clamp(seg + 2, 0, controlPoints.Length - 1);

            return CatmullRom(
                controlPoints[i0].position,
                controlPoints[i1].position,
                controlPoints[i2].position,
                controlPoints[i3].position,
                localT
            );
        }
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

        // Build rings using stable tangents (central differences)
        for (int i = 0; i < pointCount; i++)
        {
            Vector3 prev, next;
            if (isClosed)
            {
                int iPrev = (i - 1 + pointCount) % pointCount;
                int iNext = (i + 1) % pointCount;
                prev = centerPoints[iPrev];
                next = centerPoints[iNext];
            }
            else
            {
                int iPrev = Mathf.Max(0, i - 1);
                int iNext = Mathf.Min(pointCount - 1, i + 1);
                prev = centerPoints[iPrev];
                next = centerPoints[iNext];
            }

            Vector3 current = centerPoints[i];
            Vector3 tangent = next - prev;
            if (tangent.sqrMagnitude < 1e-8f) tangent = Vector3.forward; // fallback
            tangent.Normalize();

            Vector3 right = Vector3.Cross(Vector3.up, tangent).normalized;

            // lane slab
            Vector3 leftOuter  = current - right * totalWidth * 0.5f;
            Vector3 rightOuter = current + right * totalWidth * 0.5f;

            // Top
            vertices.Add(leftOuter);                    // 0
            vertices.Add(rightOuter);                   // 1
            // Bottom (thickness)
            vertices.Add(leftOuter - Vector3.up * trackThickness); // 2
            vertices.Add(rightOuter - Vector3.up * trackThickness);// 3

            float uvY = (pointCount > 1) ? i / (float)(pointCount - 1) : 0f;
            uvs.Add(new Vector2(0, uvY));
            uvs.Add(new Vector2(1, uvY));
            uvs.Add(new Vector2(0, uvY));
            uvs.Add(new Vector2(1, uvY));
        }

        // Connect rings
        int last = isClosed ? pointCount : pointCount - 1;
        for (int i = 0; i < last; i++)
        {
            int current = i * 4;
            int next = (i + 1) % pointCount * 4;
            if (!isClosed && i == pointCount - 1) break;

            // Top surface
            triangles.Add(current + 0);
            triangles.Add(next    + 0);
            triangles.Add(current + 1);

            triangles.Add(current + 1);
            triangles.Add(next    + 0);
            triangles.Add(next    + 1);

            // Bottom surface
            triangles.Add(current + 2);
            triangles.Add(current + 3);
            triangles.Add(next    + 2);

            triangles.Add(current + 3);
            triangles.Add(next    + 3);
            triangles.Add(next    + 2);

            // Left side
            triangles.Add(current + 0);
            triangles.Add(current + 2);
            triangles.Add(next    + 0);

            triangles.Add(next    + 0);
            triangles.Add(current + 2);
            triangles.Add(next    + 2);

            // Right side
            triangles.Add(current + 1);
            triangles.Add(next    + 1);
            triangles.Add(current + 3);

            triangles.Add(next    + 1);
            triangles.Add(next    + 3);
            triangles.Add(current + 3);
        }

        // End caps for OPEN tracks
        if (!isClosed && pointCount >= 2)
        {
            int start = 0;                     // first ring base index
            int end   = (pointCount - 1) * 4;  // last ring base index

            // START CAP (top)
            triangles.Add(start + 0);
            triangles.Add(start + 1);
            triangles.Add(start + 2);

            // START CAP (bottom)
            triangles.Add(start + 1);
            triangles.Add(start + 3);
            triangles.Add(start + 2);

            // END CAP (top)
            triangles.Add(end + 0);
            triangles.Add(end + 2);
            triangles.Add(end + 1);

            // END CAP (bottom)
            triangles.Add(end + 1);
            triangles.Add(end + 2);
            triangles.Add(end + 3);
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
        if (dividerObject == null)
        {
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

        float dividerWidth = 0.05f;
        float dividerHeight = 0.02f;

        List<Vector3> vertices = new();
        List<int> triangles = new();

        for (int i = 0; i < centerPoints.Count; i++)
        {
            Vector3 current = centerPoints[i];
            Vector3 next = centerPoints[Mathf.Min(i + 1, centerPoints.Count - 1)];
            if (isClosed && i == centerPoints.Count - 1) next = centerPoints[0];

            Vector3 forward = (next - current).sqrMagnitude > 1e-6f ? (next - current).normalized : Vector3.forward;
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            Vector3 leftPos = current - right * dividerWidth / 2f;
            Vector3 rightPos = current + right * dividerWidth / 2f;

            vertices.Add(leftPos + Vector3.up * dividerHeight);
            vertices.Add(rightPos + Vector3.up * dividerHeight);
        }

        int last = isClosed ? centerPoints.Count : centerPoints.Count - 1;
        for (int i = 0; i < last; i++)
        {
            int current = i * 2;
            int next = (i + 1) % centerPoints.Count * 2;
            if (!isClosed && i == centerPoints.Count - 1) break;

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

    // Gets a point on the track at normalized position t (0 to 1)
    public Vector3 GetTrackPosition(float t)
    {
        return GetPointOnCurve(isClosed ? Mathf.Repeat(t, 1f) : Mathf.Clamp01(t));
    }

    // Get position offset to left or right for lanes
    public Vector3 GetLanePosition(float t, bool leftLane)
    {
        Vector3 centerPos = GetTrackPosition(t);
        float nextT = Mathf.Clamp01(t + 0.001f);
        if (isClosed) nextT = Mathf.Repeat(t + 0.001f, 1f);

        Vector3 nextPos = GetTrackPosition(nextT);

        Vector3 forward = (nextPos - centerPos).sqrMagnitude > 1e-6f ? (nextPos - centerPos).normalized : Vector3.forward;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        float offset = laneWidth / 2f;
        return centerPos + right * (leftLane ? -offset : offset);
    }

    // Find the closest normalized t on THIS track to a given world position
    public float FindClosestT(Vector3 worldPos, int samples = 512)
    {
        samples = Mathf.Max(16, samples);
        float bestT = 0f;
        float bestDist = float.MaxValue;

        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            Vector3 p = GetTrackPosition(t);
            float d = (p - worldPos).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                bestT = t;
            }
        }
        return bestT;
    }
}
