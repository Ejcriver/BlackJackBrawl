using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            StartCoroutine(WaitAndAssignSpawn());
        }
    }

    private System.Collections.IEnumerator WaitAndAssignSpawn()
    {
        float timeout = 3f; // seconds
        float timer = 0f;
        while (PlayerSpawnManager.Instance == null && timer < timeout)
        {
            yield return null;
            timer += Time.deltaTime;
        }
        if (PlayerSpawnManager.Instance != null)
        {
            Vector3 spawnPos = PlayerSpawnManager.Instance.GetRandomSpawnPosition();
            Quaternion spawnRot = PlayerSpawnManager.Instance.GetRandomSpawnRotation();
            Debug.Log($"[PlayerController] Spawning player at {spawnPos} with rotation {spawnRot.eulerAngles}");
            transform.position = spawnPos;
            transform.rotation = spawnRot;
        }
        else
        {
            Debug.LogWarning("[PlayerController] PlayerSpawnManager.Instance is STILL null after waiting!");
        }
    }
    public float moveSpeed = 5f;
    public float mouseSensitivity = 100f;
    public Transform cameraTransform; // Assign your player camera here
    private Rigidbody rb;
    private float yRotation = 0f;
    private float xRotation = 0f;
    private bool movementEnabled = true;

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        Debug.Log($"[PlayerController] IsOwner: {IsOwner}, IsLocalPlayer: {IsLocalPlayer}, OwnerClientId: {NetworkObject.OwnerClientId}, LocalClientId: {NetworkManager.Singleton.LocalClientId}", this);
        // Only the local player controls their movement
        if (!IsOwner)
        {
            // Optionally, disable camera/audio for non-owners
            if (GetComponentInChildren<Camera>())
                GetComponentInChildren<Camera>().enabled = false;
        }
        else
        {
            // Hide and lock the cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private Vector3 movementInput;

    private void Update()
    {
        if (!IsOwner || !movementEnabled) return;
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        movementInput = new Vector3(h, 0, v).normalized;

        // Mouse look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -45f, 75f);

        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !movementEnabled) return;
        if (movementInput.sqrMagnitude > 0.01f && cameraTransform != null)
        {
            // Get the camera's forward and right directions, ignoring vertical tilt
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0;
            camForward.Normalize();
            Vector3 camRight = cameraTransform.right;
            camRight.y = 0;
            camRight.Normalize();

            // Calculate movement relative to camera
            Vector3 move = (camForward * movementInput.z + camRight * movementInput.x) * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + move);
        }
    }
}
