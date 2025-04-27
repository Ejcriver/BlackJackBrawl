using UnityEngine;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    public float moveSpeed = 5f;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        // Only the local player controls their movement
        if (!IsOwner)
        {
            // Optionally, disable camera/audio for non-owners
            if (GetComponentInChildren<Camera>())
                GetComponentInChildren<Camera>().enabled = false;
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        HandleMovement();
    }

    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 move = new Vector3(h, 0, v);
        rb.velocity = move * moveSpeed + new Vector3(0, rb.velocity.y, 0);
    }
}
