using UnityEngine;

public class TouchJump : MonoBehaviour
{
    private Rigidbody rb;
    private float jumpForce = 600f;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        //For mobile/tablet
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            rb.AddForce(new Vector3(0f, jumpForce, 0f), ForceMode.Impulse);
        }

        //For mouse
        if (Input.GetMouseButtonDown(0))
        {
            rb.AddForce(new Vector3(0f, jumpForce, 0f), ForceMode.Impulse);
        }
    }
}
