using UnityEngine;
using UnityEngine.UIElements;

public class ShopUIController : MonoBehaviour
{
    public UIDocument shopUIDocument;
    private Label shopChipsLabel;
    private VisualElement shopRoot;
    private Button closeShopButton;

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
            if (closeShopButton != null)
                closeShopButton.clicked += HideShop;
            UpdateShopChips();
        }
    }

    private void OnDisable()
    {
        if (closeShopButton != null)
            closeShopButton.clicked -= HideShop;
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
}
