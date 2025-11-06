using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Vuforia;

public class LaneSnapper : MonoBehaviour
{
    public enum LaneSide { Left, Right }

    [Header("References")]
    [Tooltip("Your Track root that has TrackGenerator (anchored under the track ImageTarget)")]
    [SerializeField] TrackGenerator track;
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
    [Tooltip("Rotate the card; we read its yaw and snap to 0/180")]
    [SerializeField] bool quantizeRotation180 = true;

    [Header("Placement Limits")]
    [Tooltip("Maximum number of objects allowed. 0 = no limit")]
    [SerializeField] int maxPlacements = 10;
    [Tooltip("If objects lack Renderers, fall back to this min world-space separation (meters)")]
    [SerializeField, Range(0f, 2f)] float minSeparationDistance = 0.30f;
    [Tooltip("Extra world-space padding when checking Renderer bounds overlap (meters)")]
    [SerializeField, Range(0f, 0.2f)] float boundsPadding = 0.01f;

    [Header("UI")]
    [SerializeField] Canvas worldCanvas;
    [SerializeField] TMP_Text feedbackText;
    [SerializeField] Button confirmButton;
    [SerializeField] TMP_Text laneBadgeText;

    ObserverBehaviour targetObserver;

    bool isLocked;
    LaneSide currentLane;
    float currentT;
    float currentYawStepDeg;
    Quaternion laneRot;

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
            SetLocked(false);
            UpdateOnTargetVisual(true);
            UpdatePreviewVisual(false);
            SetFeedback("Point your camera to the marker.");
            SetConfirmVisible(false);
            return;
        }

        Vector3 cardPos = transform.position;
        float t = FindClosestT(cardPos, curveSamples);

        Vector3 leftPos = track.GetLanePosition(t, true);
        Vector3 rightPos = track.GetLanePosition(t, false);

        float dL = Vector3.Distance(cardPos, leftPos);
        float dR = Vector3.Distance(cardPos, rightPos);

        bool canLock = Mathf.Min(dL, dR) <= snapDistance;

        if (canLock)
        {
            LaneSide lane = dL <= dR ? LaneSide.Left : LaneSide.Right;
            Vector3 targetLanePos = lane == LaneSide.Left ? leftPos : rightPos;

            const float dt = 1f / 2048f;
            Vector3 p0 = track.GetTrackPosition(t);
            Vector3 p1 = track.GetTrackPosition(t + dt);
            Vector3 fwd = (p1 - p0).sqrMagnitude > 1e-8f ? (p1 - p0).normalized : transform.forward;
            laneRot = Quaternion.LookRotation(fwd, Vector3.up);

            float cardYaw = Quaternion.LookRotation(transform.forward, Vector3.up).eulerAngles.y;
            currentYawStepDeg = quantizeRotation180 ? Quantize180(cardYaw) : Mathf.Repeat(cardYaw, 360f);

            SetLocked(true);
            currentLane = lane;
            currentT = t;

            if (previewVisual)
            {
                // Move/rotate preview into candidate pose (so our overlap check uses real bounds)
                previewVisual.position = Vector3.Lerp(previewVisual.position, targetLanePos, followTightness);
                previewVisual.rotation = laneRot * Quaternion.Euler(0f, currentYawStepDeg, 0f);
            }

            UpdateOnTargetVisual(false);
            UpdatePreviewVisual(true);
            SetLaneBadge(lane);

            // --- NEW: gate placement by limit + overlap
            var parent = GetPlacedParentMaybe();
            int placedCount = CountPlaced(parent);
            bool limitReached = maxPlacements > 0 && placedCount >= maxPlacements;
            bool overlaps = previewVisual ? WouldOverlap(previewVisual, parent) : false;

            if (limitReached)
            {
                SetFeedback($"Limit reached ({placedCount}/{maxPlacements}). Remove one to place more.");
                SetConfirmVisible(false);
            }
            else if (overlaps)
            {
                SetFeedback("Can't place here: overlaps another object. Move along the lane.");
                SetConfirmVisible(false);
            }
            else
            {
                SetFeedback("You can rotate by 180°\nThen tap 'Place'");
                SetConfirmVisible(true);
            }
        }
        else
        {
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
        float step = Mathf.Round(yawDeg / 180f) * 180f;
        return (step % 360f + 360f) % 360f;
    }

    void ConfirmPlacement()
    {
        if (!isLocked || previewVisual == null) return;

        Transform parent = placedObjectsParent != null ? placedObjectsParent : EnsurePlacedParent();

        // --- NEW: enforce cap right before committing
        int placedCount = CountPlaced(parent);
        if (maxPlacements > 0 && placedCount >= maxPlacements)
        {
            SetFeedback($"Limit reached ({placedCount}/{maxPlacements}).");
            return;
        }

        // --- NEW: block overlapping commit
        if (WouldOverlap(previewVisual, parent))
        {
            SetFeedback("Can't place here: overlaps another object. Move along the lane.");
            return;
        }

        var baked = Instantiate(previewVisual.gameObject, parent);
        baked.transform.SetPositionAndRotation(previewVisual.position, previewVisual.rotation);
        
        // Store metadata to later rebuild in 'Race' track
        var meta = baked.AddComponent<PlacedObjectMetadata>();
        meta.t = currentT;
        meta.isLeftLane = currentLane == LaneSide.Left;
        meta.yawOffset = currentYawStepDeg;
        meta.prefabName = previewVisual.name;
        
        baked.name = previewVisual.name + "_Placed";

        SetLocked(false);
        UpdateOnTargetVisual(true);
        UpdatePreviewVisual(false);
        SetLaneBadgeVisible(false);
        SetConfirmVisible(false);

        placedCount++;
        if (maxPlacements > 0)
            SetFeedback($"Placed! ({placedCount}/{maxPlacements})");
        else
            SetFeedback("Placed! You can move the card to add more.");
    }

    // ---------- Helpers: limits & overlap ----------

    Transform EnsurePlacedParent()
    {
        var t = track.transform.Find("PlacedObjects");
        if (t != null) return t;
        var go = new GameObject("PlacedObjects");
        go.transform.SetParent(track.transform, false);
        return go.transform;
    }

    Transform GetPlacedParentMaybe()
    {
        // Do NOT create until first placement; we only need it for counting/overlap checks.
        var t = placedObjectsParent ? placedObjectsParent : track.transform.Find("PlacedObjects");
        return t;
    }

    int CountPlaced(Transform parent)
    {
        if (parent == null) return 0;
        int c = 0;
        for (int i = 0; i < parent.childCount; i++)
        {
            if (parent.GetChild(i).GetComponent<PlacedObjectMetadata>() != null) c++;
        }
        return c;
    }

    bool WouldOverlap(Transform candidate, Transform parent)
    {
        if (parent == null) return false;

        // Try precise (Renderer bounds) first
        if (TryGetWorldBounds(candidate, out Bounds candBounds))
        {
            // Inflate slightly to be conservative (positive expands)
            candBounds.Expand(boundsPadding);

            for (int i = 0; i < parent.childCount; i++)
            {
                var other = parent.GetChild(i);
                if (other == candidate) continue;
                if (!other.gameObject.activeInHierarchy) continue;

                if (TryGetWorldBounds(other, out Bounds otherBounds))
                {
                    otherBounds.Expand(boundsPadding);
                    if (candBounds.Intersects(otherBounds))
                        return true;
                }
                else
                {
                    // Fallback to distance if the other has no renderers
                    if (Vector3.Distance(candidate.position, other.position) < minSeparationDistance)
                        return true;
                }
            }
            return false;
        }

        // Fallback: no renderers on candidate — use distance check
        for (int i = 0; i < parent.childCount; i++)
        {
            var other = parent.GetChild(i);
            if (!other.gameObject.activeInHierarchy) continue;
            if (Vector3.Distance(candidate.position, other.position) < minSeparationDistance)
                return true;
        }
        return false;
    }

    bool TryGetWorldBounds(Transform root, out Bounds bounds)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return true;
        }
        bounds = default;
        return false;
    }

    // ---------- Existing helpers & UI ----------

    void SetLocked(bool val) => isLocked = val;

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
