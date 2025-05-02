using UnityEngine;
using UnityEngine.UIElements;

public class ShopUIController : MonoBehaviour
{
    public UIDocument shopUIDocument;
    private Label shopChipsLabel;
    private VisualElement shopRoot;
    private Button closeShopButton;
    private Button buyMaxHPButton;
    private Button buyDoubleDownButton;
    private Button buyStealButton; // For power cards
    private Button showDeckButton; // New: Show Deck button

    private void Awake()
    {
        if (shopUIDocument == null)
            shopUIDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (shopUIDocument != null)
        {
            var root = shopUIDocument.rootVisualElement;
            shopRoot = root.Q<VisualElement>("ShopRoot");
            shopChipsLabel = root.Q<Label>("ShopChipsLabel");
            closeShopButton = root.Q<Button>("CloseShopButton");
            showDeckButton = root.Q<Button>("ShowDeckButton");
            if (showDeckButton != null)
                showDeckButton.clicked += OnShowDeckClicked;
            if (closeShopButton != null)
                closeShopButton.clicked += HideShop;
            if (buyMaxHPButton != null)
                buyMaxHPButton.clicked += OnBuyMaxHP;
            buyDoubleDownButton = root.Q<Button>("BuyDoubleDownButton");
            buyStealButton = root.Q<Button>("BuyStealButton");
            if (buyDoubleDownButton != null)
                buyDoubleDownButton.clicked += OnBuyDoubleDown;
            if (buyStealButton != null)
                buyStealButton.clicked += OnBuySteal;
            UpdateShopChips();
        }
    }

    private void OnDisable()
    {
        if (closeShopButton != null)
            closeShopButton.clicked -= HideShop;
        if (buyMaxHPButton != null)
            buyMaxHPButton.clicked -= OnBuyMaxHP;
        if (buyDoubleDownButton != null)
            buyDoubleDownButton.clicked -= OnBuyDoubleDown;
        if (buyStealButton != null)
            buyStealButton.clicked -= OnBuySteal;
        if (showDeckButton != null)
            showDeckButton.clicked -= OnShowDeckClicked;
    }

    public void ShowShop()
    {
        if (shopRoot != null)
            shopRoot.style.display = DisplayStyle.Flex;
        UpdateShopChips();
    }

    public void HideShop()
    {
        if (shopRoot != null)
            shopRoot.style.display = DisplayStyle.None;
        // Re-enable player movement and lock/hide cursor
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var pc = player.GetComponent<PlayerController>();
            if (pc != null)
                pc.SetMovementEnabled(true);
        }
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
    }

    public void UpdateShopChips()
    {
        // Get the chip count from the NetworkBlackjackManager
        var blackjackManager = FindFirstObjectByType<NetworkBlackjackManager>();
        if (blackjackManager != null)
        {
            ulong localClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            int localPlayerIdx = blackjackManager.PlayerIds.IndexOf(localClientId);
            int chips = (localPlayerIdx >= 0 && localPlayerIdx < blackjackManager.PlayerChips.Count) ? blackjackManager.PlayerChips[localPlayerIdx] : 0;
            if (shopChipsLabel != null)
                shopChipsLabel.text = $"Chips: {chips}";
        }
    }

    private void OnBuyMaxHP()
    {
        var blackjackManager = FindFirstObjectByType<NetworkBlackjackManager>();
        if (blackjackManager != null)
        {
            ulong localClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            int localPlayerIdx = blackjackManager.PlayerIds.IndexOf(localClientId);
            if (localPlayerIdx >= 0 && localPlayerIdx < blackjackManager.PlayerChips.Count)
            {
                int chips = blackjackManager.PlayerChips[localPlayerIdx];
                if (chips >= 10)
                {
                    // Call ServerRpc to process purchase
                    blackjackManager.BuyMaxHPServerRpc(localClientId);
                    UpdateShopChips();
                }
            }
        }
    }

    // Demo: Buy Double Down power card (powerId 1)
    private void OnBuyDoubleDown()
    {
        var blackjackManager = FindFirstObjectByType<NetworkBlackjackManager>();
        if (blackjackManager != null)
        {
            ulong localClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            int localPlayerIdx = blackjackManager.PlayerIds.IndexOf(localClientId);
            if (localPlayerIdx >= 0 && localPlayerIdx < blackjackManager.PlayerChips.Count)
            {
                int chips = blackjackManager.PlayerChips[localPlayerIdx];
                if (chips >= 15)
                {
                    blackjackManager.BuyPowerCardServerRpc(localClientId, 1); // 1 = Double Down
                    UpdateShopChips();
                }
                else
                {
                    Debug.Log("[ShopUI] Not enough chips to buy Double Down.");
                }
            }
        }
    }

    private void OnBuySteal()
    {
        var blackjackManager = FindFirstObjectByType<NetworkBlackjackManager>();
        if (blackjackManager != null)
        {
            ulong localClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            int localPlayerIdx = blackjackManager.PlayerIds.IndexOf(localClientId);
            if (localPlayerIdx >= 0 && localPlayerIdx < blackjackManager.PlayerChips.Count)
            {
                int chips = blackjackManager.PlayerChips[localPlayerIdx];
                if (chips >= 15)
                {
                    blackjackManager.BuyPowerCardServerRpc(localClientId, 2); // 2 = Steal
                    UpdateShopChips();
                }
                else
                {
                    Debug.Log("[ShopUI] Not enough chips to buy Steal.");
                }
            }
        }
    }

    // Show Deck button click handler for Shop UI
    private void OnShowDeckClicked()
    {
        var blackjackUI = FindFirstObjectByType<BlackjackUIController>();
        if (blackjackUI != null)
        {
            var method = typeof(BlackjackUIController).GetMethod("OnShowDeckClicked", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (method != null)
                method.Invoke(blackjackUI, null);
            else
                Debug.LogWarning("[ShopUI] Could not find OnShowDeckClicked method in BlackjackUIController.");
        }
        else
        {
            Debug.LogWarning("[ShopUI] Could not find BlackjackUIController instance to show deck popup.");
        }
    }
}

