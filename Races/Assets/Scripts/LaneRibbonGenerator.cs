using UnityEngine;

[RequireComponent(typeof(TrackGenerator))]
public class LaneRibbonGenerator : MonoBehaviour
{
    [Header("Lane Ribbons")]
    [SerializeField] Material leftLaneMat;
    [SerializeField] Material rightLaneMat;
    [SerializeField, Range(0.02f, 0.3f)] float ribbonWidth = 0.08f;
    [SerializeField, Range(64, 2048)] int samples = 512;

    LineRenderer leftLR, rightLR;
    TrackGenerator track;

    void Awake()
    {
        track = GetComponent<TrackGenerator>();
        leftLR = CreateLR("LeftLaneRibbon", leftLaneMat);
        rightLR = CreateLR("RightLaneRibbon", rightLaneMat);
    }

    LineRenderer CreateLR(string name, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.loop = true;
        lr.alignment = LineAlignment.View; // flat ribbon look from AR camera
        lr.textureMode = LineTextureMode.Stretch;
        lr.widthMultiplier = ribbonWidth;
        lr.material = mat;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        return lr;
    }

    void LateUpdate()
    {
        if (track == null || track.controlPoints == null || track.controlPoints.Length < 3) return;

        leftLR.positionCount = samples;
        rightLR.positionCount = samples;

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            Vector3 leftPos  = track.GetLanePosition(t, true);
            Vector3 rightPos = track.GetLanePosition(t, false);

            // Small upward offset to avoid z-fighting
            float yOffset = 0.05f; // 1 cm above the track
            leftPos.y  += yOffset;
            rightPos.y += yOffset;

            leftLR.SetPosition(i, leftPos);
            rightLR.SetPosition(i, rightPos);
        }
    }
}
