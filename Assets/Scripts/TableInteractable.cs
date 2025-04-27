using UnityEngine;
using UnityEngine.UIElements;

using UnityEngine.UI;

public class TableInteractable : MonoBehaviour
{
    [Tooltip("The UIDocument to show when the player interacts with this table.")]
    public UnityEngine.UIElements.UIDocument blackjackUIDocument;
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
            ShowBlackjackUI();
        }
        // Allow closing UI with Escape key
        if (blackjackUIDocument != null && blackjackUIDocument.gameObject.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseBlackjackUI();
        }
    }

    private void ShowBlackjackUI()
    {
        if (blackjackUIDocument != null)
        {
            Debug.Log($"[TableInteractable] Showing blackjack UI document for: {gameObject.name}");
            blackjackUIDocument.gameObject.SetActive(true);
            // Lock cursor and pause movement
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
            if (player != null)
            {
                var pc = player.GetComponent<PlayerController>();
                if (pc != null)
                    pc.SetMovementEnabled(false);
            }
            // Register close button event if not already registered
            var closeBtn = blackjackUIDocument.rootVisualElement.Q<UnityEngine.UIElements.Button>("CloseButton");
            if (closeBtn != null)
            {
                closeBtn.clicked -= CloseBlackjackUI; // Remove any duplicate listeners
                closeBtn.clicked += CloseBlackjackUI;
            }
        }
        else
        {
            Debug.LogWarning($"[TableInteractable] blackjackUIDocument is not assigned on {gameObject.name}");
        }
    }

    private void CloseBlackjackUI()
    {
        if (blackjackUIDocument != null)
        {
            blackjackUIDocument.gameObject.SetActive(false);
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
            if (player != null)
            {
                var pc = player.GetComponent<PlayerController>();
                if (pc != null)
                    pc.SetMovementEnabled(true);
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
            if (blackjackUIDocument != null)
            {
                blackjackUIDocument.gameObject.SetActive(false);
                // Unlock cursor and resume movement
                UnityEngine.Cursor.lockState = CursorLockMode.Locked;
                UnityEngine.Cursor.visible = false;
                if (player != null)
                {
                    var pc = player.GetComponent<PlayerController>();
                    if (pc != null)
                        pc.SetMovementEnabled(true);
                }
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
