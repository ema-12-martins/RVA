using UnityEngine;

public class RacerAnimator : MonoBehaviour
{
    //To receive the color of the car
    public string color;

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

    [Header("Jump Settings")]
    public float jumpHeight = 1f;
    public float jumpDuration = 0.5f;
    private bool isJumping = false;
    private float jumpTimer = 0f;
    private float jumpOffset = 0f;


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


        // Update jump
        if (isJumping)
        {
            jumpTimer += Time.deltaTime;
            float t = jumpTimer / jumpDuration;
            jumpOffset = 4 * jumpHeight * t * (1 - t);

            if (jumpTimer >= jumpDuration)
            {
                isJumping = false;
                jumpOffset = 0f;
            }
        }
        else
        {
            jumpOffset = 0.3f; 
        }

        // Aplicar posição final
        transform.position = targetPos + Vector3.up * jumpOffset;


        // Calculate rotation to face forward
        float lookAheadT = currentPosition + 0.01f;
        Vector3 lookAheadPos = track.GetLanePosition(lookAheadT, leftLane);
        Vector3 forward = (lookAheadPos - targetPos).normalized;
        
        if (forward != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        //Detect input of the mouse or click on the screen
        if (!isJumping)
        {
            bool touch = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
            bool click = Input.GetMouseButtonDown(0);
            if ((touch || click) && GameData.carColor == color)
            {
                isJumping = true;
                jumpTimer = 0f;
            }
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