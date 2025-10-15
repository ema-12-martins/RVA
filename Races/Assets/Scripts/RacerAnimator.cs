using UnityEngine;

public class RacerAnimator : MonoBehaviour
{
    [Header("Track Reference")]
    public TrackGenerator track;
    
    [Header("Racer Settings")]
    [Tooltip("Which lane: true = left, false = right")]
    public bool leftLane = true;
    
    [Range(0.1f, 10f)]
    [Tooltip("Speed multiplier")]
    public float speed = 1f;
    
    [Range(0f, 1f)]
    [Tooltip("Starting position on track (0 to 1)")]
    public float startPosition = 0f;
    
    [Header("Visuals")]
    public Color racerColor = Color.green;
    
    private float currentPosition;
    private MeshRenderer meshRenderer;
    
    void Start()
    {
        currentPosition = startPosition;
        
        // Setup cube appearance
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = racerColor;
            meshRenderer.material = mat;
        }
        
        // Validate track reference
        if (track == null)
        {
            Debug.LogError("RacerAnimator: No track assigned!");
        }
    }
    
    void Update()
    {
        if (track == null) return;
        
        // Update position along track
        currentPosition += speed * Time.deltaTime * 0.1f;
        currentPosition = Mathf.Repeat(currentPosition, 1f);
        
        // Get position on the track
        Vector3 targetPos = track.GetLanePosition(currentPosition, leftLane);
        transform.position = targetPos + Vector3.up * 0.3f; // Slight elevation
        
        // Calculate rotation to face forward
        float lookAheadT = currentPosition + 0.01f;
        Vector3 lookAheadPos = track.GetLanePosition(lookAheadT, leftLane);
        Vector3 forward = (lookAheadPos - targetPos).normalized;
        
        if (forward != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }
    
    // Reset racer to start position
    public void ResetPosition()
    {
        currentPosition = startPosition;
    }
    
    // Set position manually
    public void SetPosition(float t)
    {
        currentPosition = Mathf.Clamp01(t);
    }
}