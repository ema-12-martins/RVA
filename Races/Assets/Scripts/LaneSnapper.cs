using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Vuforia;

/// <summary>
/// Attach to the ImageTarget of a placeable object (e.g., ConeTarget).
/// Shows a preview that snaps to the nearest lane when within threshold,
/// follows the lane while you stay close, supports 90° rotation steps by rotating the card,
/// and confirms to bake a copy into the track prefab.
/// </summary>
public class LaneSnapper : MonoBehaviour
{
    public enum LaneSide { Left, Right }

    [Header("References")]
    [Tooltip("Your Track root that has TrackGenerator (anchored under the track ImageTarget)")]
    [SerializeField] TrackGenerator track;   // uses GetTrackPosition/GetLanePosition (from your script)
    [Tooltip("Optional parent under the track where confirmed objects are stored")]
    [SerializeField] Transform placedObjectsParent; 

    [Header("Object Visuals")]
    [Tooltip("The 3D object shown on top of THIS ImageTarget when not locked")]
    [SerializeField] Transform onTargetVisual;
    [Tooltip("A separate preview instance that moves along the lane when locked")]
    [SerializeField] Transform previewVisual;

    [Header("Locking")]
    [Tooltip("Max distance (in meters) from a lane center to snap/lock")]
    [SerializeField, Range(0.01f, 0.5f)] float snapDistance = 0.12f;
    [Tooltip("How tightly the preview follows while locked (1 = hard snap)")]
    [SerializeField, Range(0.25f, 1f)] float followTightness = 0.85f;
    [Tooltip("Curve sampling resolution for finding closest t on the track")]
    [SerializeField, Range(64, 2048)] int curveSamples = 512;

    [Header("Rotation")]
    [Tooltip("Rotate the card; we read its yaw and snap to 0/90/180/270")]
    [SerializeField] bool quantizeRotation90 = true;

    [Header("UI")]
    [SerializeField] Canvas worldCanvas;     // world-space canvas near the preview
    [SerializeField] TMP_Text feedbackText;
    [SerializeField] Button confirmButton;
    [SerializeField] TMP_Text laneBadgeText; // e.g., "LEFT LANE" / "RIGHT LANE"

    ObserverBehaviour targetObserver;

    bool isLocked;
    LaneSide currentLane;
    float currentT;            // current param along the curve
    float currentYawStepDeg;   // 0,90,180,270
    Quaternion laneRot;        // forward-aligned rotation from track tangent

    void Awake()
    {
        targetObserver = GetComponent<ObserverBehaviour>();
        if (confirmButton) confirmButton.onClick.AddListener(ConfirmPlacement);
        SetLocked(false);
    }

    void Update()
    {
        if (track == null || targetObserver == null) return;

        bool tracked = IsTracked(targetObserver.TargetStatus);
        if (!tracked)
        {
            // Lost tracking: ensure we appear as "not locked"
            SetLocked(false);
            UpdateOnTargetVisual(true);
            UpdatePreviewVisual(false);
            SetFeedback("Point your camera to the marker.");
            return;
        }

        // Where is the card in world space?
        Vector3 cardPos = transform.position;

        // Find closest parameter t along the track center
        float t = FindClosestT(cardPos, curveSamples);

        // Lane centers at t
        Vector3 leftPos = track.GetLanePosition(t, true);
        Vector3 rightPos = track.GetLanePosition(t, false);

        // Which lane is closer?
        float dL = Vector3.Distance(cardPos, leftPos);
        float dR = Vector3.Distance(cardPos, rightPos);

        bool canLock = Mathf.Min(dL, dR) <= snapDistance;

        if (canLock)
        {
            LaneSide lane = dL <= dR ? LaneSide.Left : LaneSide.Right;
            Vector3 targetLanePos = lane == LaneSide.Left ? leftPos : rightPos;

            // Compute forward/tangent for rotation alignment
            const float dt = 1f / 2048f;
            Vector3 p0 = track.GetTrackPosition(t);
            Vector3 p1 = track.GetTrackPosition(t + dt);
            Vector3 fwd = (p1 - p0).sqrMagnitude > 1e-8f ? (p1 - p0).normalized : transform.forward;
            laneRot = Quaternion.LookRotation(fwd, Vector3.up);

            // Read card yaw & quantize to 90° steps (if enabled)
            float cardYaw = Quaternion.LookRotation(transform.forward, Vector3.up).eulerAngles.y;
            currentYawStepDeg = quantizeRotation90 ? Quantize180(cardYaw) : Mathf.Repeat(cardYaw, 360f);

            // Enter/keep locked follow
            SetLocked(true);
            currentLane = lane;
            currentT = t;

            // Smooth follow of preview
            if (previewVisual)
            {
                // move
                previewVisual.position = Vector3.Lerp(previewVisual.position, targetLanePos, followTightness);
                // rotate: lane forward * stepped yaw around up
                previewVisual.rotation = laneRot * Quaternion.Euler(0f, currentYawStepDeg, 0f);
            }

            // UI feedback
            UpdateOnTargetVisual(false);
            UpdatePreviewVisual(true);
            SetLaneBadge(lane);
            SetFeedback($"You can rotate by 180º\nTo confirm tap 'Place'");
            SetConfirmVisible(true);
        }
        else
        {
            // Not close enough: unlocked, show object on the card, hide preview
            if (!isLocked)
            {
                UpdateOnTargetVisual(true);
                UpdatePreviewVisual(false);
            }
            SetLocked(false);
            SetLaneBadgeVisible(false);
            SetFeedback("Move the marker close to the lane center to snap.");
            SetConfirmVisible(false);
        }
    }

    float FindClosestT(Vector3 worldPos, int samples)
    {
        // Brute-force sample the curve (center line) via your TrackGenerator API. :contentReference[oaicite:1]{index=1}
        float bestT = 0f;
        float bestD2 = float.MaxValue;

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            Vector3 c = track.GetTrackPosition(t);
            float d2 = (c - worldPos).sqrMagnitude;
            if (d2 < bestD2)
            {
                bestD2 = d2;
                bestT = t;
            }
        }
        return bestT;
    }

    float Quantize180(float yawDeg)
    {
        // Return nearest multiple of 180
        float step = Mathf.Round(yawDeg / 180f) * 180f;
        // Normalize to [0,360)
        return (step % 360f + 360f) % 360f;
    }

    void ConfirmPlacement()
    {
        if (!isLocked || previewVisual == null) return;

        // Create (or ensure) a parent for placed objects
        Transform parent = placedObjectsParent != null ? placedObjectsParent : EnsurePlacedParent();

        // Bake a copy at the preview pose
        var baked = Instantiate(previewVisual.gameObject, parent);
        baked.transform.SetPositionAndRotation(previewVisual.position, previewVisual.rotation);
        baked.name = previewVisual.name + "_Placed";

        // Stay in unlocked state so user can keep using the card
        SetLocked(false);
        UpdateOnTargetVisual(true);
        UpdatePreviewVisual(false);
        SetLaneBadgeVisible(false);
        SetConfirmVisible(false);
        SetFeedback("Placed! You can move the card to add more.");
    }

    Transform EnsurePlacedParent()
    {
        var t = track.transform.Find("PlacedObjects");
        if (t != null) return t;
        var go = new GameObject("PlacedObjects");
        go.transform.SetParent(track.transform, false);
        return go.transform;
    }

    void SetLocked(bool val)
    {
        isLocked = val;
    }

    bool IsTracked(TargetStatus status)
    {
        var s = status.Status;
        return s == Status.TRACKED;
    }

    void UpdateOnTargetVisual(bool visible)
    {
        if (onTargetVisual) onTargetVisual.gameObject.SetActive(visible);
    }

    void UpdatePreviewVisual(bool visible)
    {
        if (previewVisual) previewVisual.gameObject.SetActive(visible);
        if (worldCanvas) worldCanvas.gameObject.SetActive(visible);
    }

    void SetConfirmVisible(bool visible)
    {
        if (confirmButton) confirmButton.gameObject.SetActive(visible);
    }

    void SetFeedback(string msg)
    {
        if (feedbackText) feedbackText.text = msg;
    }

    void SetLaneBadge(LaneSide lane)
    {
        if (laneBadgeText)
        {
            laneBadgeText.text = lane == LaneSide.Left ? "LEFT LANE" : "RIGHT LANE";
            SetLaneBadgeVisible(true);
        }
    }

    void SetLaneBadgeVisible(bool visible)
    {
        if (laneBadgeText) laneBadgeText.gameObject.SetActive(visible);
    }
}
