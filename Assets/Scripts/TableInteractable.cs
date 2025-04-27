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

    private bool IsLocalPlayer(GameObject playerObj)
    {
        var playerController = playerObj.GetComponent<PlayerController>();
        return playerController != null && playerController.IsOwner;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && IsLocalPlayer(other.gameObject))
        {
            Debug.Log($"[TableInteractable] Local player entered trigger: {gameObject.name}");
            isPlayerNearby = true;
            player = other.gameObject;
            if (promptPrefab != null && promptInstance == null)
            {
                promptInstance = Instantiate(promptPrefab);
                Debug.Log($"[TableInteractable] Prompt instantiated for table: {gameObject.name}");
            }
            else if (promptPrefab == null)
            {
                Debug.LogWarning($"[TableInteractable] promptPrefab is not assigned on {gameObject.name}");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && IsLocalPlayer(other.gameObject))
        {
            Debug.Log($"[TableInteractable] Local player exited trigger: {gameObject.name}");
            isPlayerNearby = false;
            player = null;
            if (blackjackUICanvas != null)
            {
                blackjackUICanvas.SetActive(false);
            }
            if (promptInstance != null)
            {
                Destroy(promptInstance);
                promptInstance = null;
                Debug.Log($"[TableInteractable] Prompt destroyed for table: {gameObject.name}");
            }
        }
    }
}
