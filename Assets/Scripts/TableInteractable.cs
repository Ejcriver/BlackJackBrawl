using UnityEngine;

using UnityEngine.UI;

public class TableInteractable : MonoBehaviour
{
    [Tooltip("The UI canvas to show when the player interacts with this table.")]
    public GameObject blackjackUICanvas;
    [Tooltip("The key used to interact with the table.")]
    public KeyCode interactKey = KeyCode.E;
    [Tooltip("How close the player must be to interact.")]
    public float interactDistance = 2.5f;

    private bool isPlayerNearby = false;
    private GameObject player;

    [Header("UI Prompt")]
    public GameObject promptPrefab; // Assign a prefab with a Canvas+Text (or TMP) in Inspector
    private GameObject promptInstance;

    void Update()
    {
        if (isPlayerNearby && Input.GetKeyDown(interactKey))
        {
            Debug.Log($"[TableInteractable] E pressed while near table: {gameObject.name}");
            if (blackjackUICanvas != null)
            {
                Debug.Log($"[TableInteractable] Showing blackjack UI canvas for: {gameObject.name}");
                blackjackUICanvas.SetActive(true);
                // Optionally, lock cursor and pause movement here
            }
            else
            {
                Debug.LogWarning($"[TableInteractable] blackjackUICanvas is not assigned on {gameObject.name}");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[TableInteractable] Player entered trigger: {gameObject.name}");
            isPlayerNearby = true;
            player = other.gameObject;
            // Show UI prompt
            if (promptPrefab != null && promptInstance == null)
            {
                promptInstance = Instantiate(promptPrefab);
                Debug.Log($"[TableInteractable] Prompt instantiated for table: {gameObject.name}");
                // If you want the prompt to follow the table in world space:
                // promptInstance.transform.position = transform.position + Vector3.up * 2f;
                // Or for screen-space, just instantiate as-is
            }
            else if (promptPrefab == null)
            {
                Debug.LogWarning($"[TableInteractable] promptPrefab is not assigned on {gameObject.name}");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[TableInteractable] Player exited trigger: {gameObject.name}");
            isPlayerNearby = false;
            player = null;
            if (blackjackUICanvas != null)
            {
                blackjackUICanvas.SetActive(false);
            }
            // Hide UI prompt
            if (promptInstance != null)
            {
                Destroy(promptInstance);
                promptInstance = null;
                Debug.Log($"[TableInteractable] Prompt destroyed for table: {gameObject.name}");
            }
        }
    }
}
