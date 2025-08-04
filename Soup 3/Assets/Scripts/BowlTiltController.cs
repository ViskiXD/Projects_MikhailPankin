using UnityEngine;

public class BowlTiltController : MonoBehaviour
{
    [Header("Bowl Tilt Settings")]
    [SerializeField] private float maxTiltAngle = 25f;
    [SerializeField] private float tiltSpeed = 70f; // Degrees per second

    private Vector3 currentRotation;

    void Start()
    {
        currentRotation = Vector3.zero;
        
        // Simple physics setup - don't mess with colliders
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = false;
            rb.isKinematic = true;
        }
    }

    void Update()
    {
        HandleInput();
        ApplyRotation();
    }

    void HandleInput()
    {
        float forwardInput = 0f;
        float sideInput = 0f;

        // W/S for forward/backward tilt
        if (Input.GetKey(KeyCode.W))
            forwardInput = 1f;
        else if (Input.GetKey(KeyCode.S))
            forwardInput = -1f;

        // A/D for left/right tilt  
        if (Input.GetKey(KeyCode.A))
            sideInput = 1f;
        else if (Input.GetKey(KeyCode.D))
            sideInput = -1f;

        // Apply rotation
        if (forwardInput != 0f || sideInput != 0f)
        {
            float rotationDelta = tiltSpeed * Time.deltaTime;
            
            currentRotation.x += forwardInput * rotationDelta;
            currentRotation.x = Mathf.Clamp(currentRotation.x, -maxTiltAngle, maxTiltAngle);

            currentRotation.z += sideInput * rotationDelta;
            currentRotation.z = Mathf.Clamp(currentRotation.z, -maxTiltAngle, maxTiltAngle);
        }
    }

    void ApplyRotation()
    {
        transform.rotation = Quaternion.Euler(currentRotation.x, 0f, currentRotation.z);
    }

    [ContextMenu("Reset Bowl")]
    public void ResetBowl()
    {
        currentRotation = Vector3.zero;
        
        // Also reset soup simulation if present
        SoupFluidController soupController = Object.FindFirstObjectByType<SoupFluidController>();
        if (soupController != null)
        {
            soupController.ResetSimulation();
        }
    }
    
    // Public property for soup controller integration
    public Vector3 CurrentTilt => currentRotation;
}