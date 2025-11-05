using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class BillboardCanvas : MonoBehaviour
{
    [Tooltip("If null, uses Camera.main")]
    public Camera targetCamera;
    [Tooltip("If true, canvas matches camera yaw only (keeps upright). If false, fully faces camera.")]
    public bool yawOnly = true;
    [Tooltip("Smooth factor. 0 = instant, 10-20 = nice smooth.")]
    [Range(0f, 30f)] public float followSpeed = 12f;

    Quaternion targetRot;

    void Start()
    {
        if (!targetCamera) targetCamera = Camera.main;
        targetRot = transform.rotation;
        var c = GetComponent<Canvas>();
        if (c && c.renderMode == RenderMode.WorldSpace && !c.worldCamera)
            c.worldCamera = targetCamera; // correct sorting
    }

    void LateUpdate()
    {
        if (!targetCamera) return;

        if (yawOnly)
        {
            // Match only camera yaw; keep canvas upright in world space
            Vector3 forward = targetCamera.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-6f) forward = Vector3.forward;
            targetRot = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }
        else
        {
            // Fully face camera
            Vector3 dir = (transform.position - targetCamera.transform.position).normalized;
            targetRot = Quaternion.LookRotation(dir, Vector3.up);
        }

        if (followSpeed <= 0f) transform.rotation = targetRot;
        else transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followSpeed);
    }
}
