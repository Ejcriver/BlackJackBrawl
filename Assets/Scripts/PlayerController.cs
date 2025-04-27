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
        rb.linearVelocity = move * moveSpeed + new Vector3(0, rb.linearVelocity.y, 0);
    }
}
