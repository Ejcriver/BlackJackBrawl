using UnityEngine;
using UnityEngine.UIElements;

public class ShopUIController : MonoBehaviour
{
    public UIDocument shopUIDocument;
    private Label shopChipsLabel;
    private VisualElement shopRoot;
    private Button closeShopButton;
    private Button buyMaxHPButton;

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
            buyMaxHPButton = root.Q<Button>("BuyMaxHPButton");
            if (closeShopButton != null)
                closeShopButton.clicked += HideShop;
            if (buyMaxHPButton != null)
                buyMaxHPButton.clicked += OnBuyMaxHP;
            UpdateShopChips();
        }
    }

    private void OnDisable()
    {
        if (closeShopButton != null)
            closeShopButton.clicked -= HideShop;
        if (buyMaxHPButton != null)
            buyMaxHPButton.clicked -= OnBuyMaxHP;
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
}
