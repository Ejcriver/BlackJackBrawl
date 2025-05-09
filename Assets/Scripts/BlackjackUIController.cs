using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Unity.Netcode;

public class BlackjackUIController : MonoBehaviour
{
    public UIDocument blackjackUIDocument;
    // public BlackjackGameLogic blackjackGameLogic; // No longer used

    private Button hitButton;
    private Button standButton;
    private Button startRoundButton;
    private Button joinTableButton;
    private Button startSessionButton;
    private VisualElement playerHandPanel;
    private VisualElement infoPanel;
    private Label messageLabel;
    private Label playerLabel; // Remove dealerLabel
    private List<Label> playerInfoLabels = new List<Label>();

    // Deck UI
    private Button showDeckButton;
    private VisualElement deckPopup;
    private ScrollView deckListScroll;
    private Button closeDeckPopupButton;

    // Stats panel fields
    private VisualElement localStatsPanel;
    private VisualElement otherStatsPanel;
    private Label handValueLabel;
    private Label hpLabel;
    private Label chipsLabel;
    private Label otherHandValueLabel;
    private Label otherHPLabel;
    private Label otherChipsLabel;


    void Awake()
    {
        if (blackjackUIDocument == null)
            blackjackUIDocument = GetComponent<UIDocument>();
    }

    void OnEnable()
    {
        // Multiplayer: subscribe to player list changes for UI updates
        var blackjackManager = FindFirstObjectByType<NetworkBlackjackManager>();
        if (blackjackManager != null)
            blackjackManager.OnPlayerListChanged += UpdateUI;

        if (blackjackUIDocument != null)
        {
            var root = blackjackUIDocument.rootVisualElement;
            hitButton = root.Q<Button>("HitButton");
            standButton = root.Q<Button>("StandButton");
            playerHandPanel = root.Q<VisualElement>("PlayerHand");
            infoPanel = root.Q<VisualElement>("InfoPanel");
            messageLabel = root.Q<Label>("MessageLabel");
            playerLabel = root.Q<Label>("PlayerLabel");
            chipsLabel = root.Q<Label>("ChipsLabel");

            // Stats Panels
            localStatsPanel = root.Q<VisualElement>("LocalStatsPanel");
            otherStatsPanel = root.Q<VisualElement>("OtherStatsPanel");
            handValueLabel = root.Q<Label>("HandValueLabel");
            hpLabel = root.Q<Label>("HPLabel");
            chipsLabel = root.Q<Label>("ChipsLabel");
            otherHandValueLabel = root.Q<Label>("OtherHandValueLabel");
            otherHPLabel = root.Q<Label>("OtherHPLabel");
            otherChipsLabel = root.Q<Label>("OtherChipsLabel");
            startRoundButton = root.Q<Button>("StartRoundButton");
            if (startRoundButton != null)
                startRoundButton.clicked += OnStartRoundClicked;

            joinTableButton = root.Q<Button>("JoinTableButton");
            if (joinTableButton != null)
                joinTableButton.clicked += OnJoinTableClicked;
            startSessionButton = root.Q<Button>("StartSessionButton");
            if (startSessionButton != null)
                startSessionButton.clicked += OnStartSessionClicked;

            if (hitButton != null)
                hitButton.clicked += OnHitClicked;
            if (standButton != null)
                standButton.clicked += OnStandClicked;

            // Add Show Deck button
            // Always re-query and re-assign showDeckButton and event handler
            if (showDeckButton != null)
                showDeckButton.clicked -= OnShowDeckClicked;
            showDeckButton = root.Q<Button>("ShowDeckButton");
            if (showDeckButton == null)
            {
                // Dynamically add if not in UXML
                showDeckButton = new Button() { name = "ShowDeckButton", text = "Show Deck" };
                showDeckButton.style.marginTop = 8;
                showDeckButton.style.width = 120;
                root.Add(showDeckButton);
            }
            showDeckButton.clicked += OnShowDeckClicked;

            // Always reset deckPopup and deckListScroll on re-enter
            deckPopup = root.Q<VisualElement>("DeckPopup");
            deckListScroll = null;
            if (deckPopup != null)
                deckListScroll = deckPopup.Q<ScrollView>("DeckListScroll");

            // Subscribe to deck event for non-hosts
            NetworkBlackjackManager.OnDeckReceivedFromHost += OnDeckReceivedFromHost;
            // Dynamically update deck popup if open
            if (blackjackManager != null)
                blackjackManager.OnPlayerListChanged += OnDeckMaybeChanged;

            // Add Deck Popup (hidden by default)
            if (deckPopup == null)
            {
                var deckPopupAsset = Resources.Load<VisualTreeAsset>("UI/DeckPopup");
                if (deckPopupAsset != null)
                {
                    deckPopup = deckPopupAsset.Instantiate();
                    deckPopup.style.display = DisplayStyle.None;
                    deckListScroll = deckPopup.Q<ScrollView>("DeckListScroll");
                    closeDeckPopupButton = deckPopup.Q<Button>("CloseDeckPopupButton");
                    if (closeDeckPopupButton != null)
                        closeDeckPopupButton.clicked += () => deckPopup.style.display = DisplayStyle.None;
                    root.Add(deckPopup);
                }
                else
                {
                    // fallback: create popup manually
                    deckPopup = new VisualElement { name = "DeckPopup" };
                    deckPopup.style.display = DisplayStyle.None;
                    deckPopup.style.flexDirection = FlexDirection.Column;
                    deckPopup.style.backgroundColor = new StyleColor(new Color(0.13f,0.13f,0.13f,0.85f));
                    deckPopup.style.paddingLeft = 12; deckPopup.style.paddingRight = 12; deckPopup.style.paddingTop = 12; deckPopup.style.paddingBottom = 12;
                    // deckPopup.style.borderRadius = 8; // Not supported in runtime UI Toolkit
                    deckPopup.style.minWidth = 220;
                    deckPopup.style.minHeight = 80;
                    var title = new Label("Your Deck") { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 16, marginBottom = 8 } };
                    deckListScroll = new ScrollView { name = "DeckListScroll", style = { height = 180, minWidth = 180, backgroundColor = new StyleColor(new Color(0.07f,0.07f,0.07f,0.7f)), marginBottom = 8 } };
// Rounded corners not supported in runtime UI Toolkit, can be added via USS if needed.
                    closeDeckPopupButton = new Button(() => deckPopup.style.display = DisplayStyle.None) { text = "Close", style = { alignSelf = Align.FlexEnd, marginTop = 8, width = 80 } };
                    deckPopup.Add(title);
                    deckPopup.Add(deckListScroll);
                    deckPopup.Add(closeDeckPopupButton);
                    root.Add(deckPopup);
                }
            }

            UpdateUI();
        }
    }

    void OnDisable()
    {
        // Multiplayer: unsubscribe
        var blackjackManager = FindFirstObjectByType<NetworkBlackjackManager>();
        if (blackjackManager != null)
            blackjackManager.OnPlayerListChanged -= UpdateUI;

        if (hitButton != null)
            hitButton.clicked -= OnHitClicked;
        if (standButton != null)
            standButton.clicked -= OnStandClicked;
        if (startRoundButton != null)
            startRoundButton.clicked -= OnStartRoundClicked;
        if (joinTableButton != null)
            joinTableButton.clicked -= OnJoinTableClicked;
        if (startSessionButton != null)
            startSessionButton.clicked -= OnStartSessionClicked;
        if (showDeckButton != null)
            showDeckButton.clicked -= OnShowDeckClicked;
        NetworkBlackjackManager.OnDeckReceivedFromHost -= OnDeckReceivedFromHost;
        var managerForUnsubscribe = FindFirstObjectByType<NetworkBlackjackManager>();
        if (managerForUnsubscribe != null)
            managerForUnsubscribe.OnPlayerListChanged -= OnDeckMaybeChanged;
    }

    // Called when the deck might have changed
    private void OnDeckMaybeChanged()
    {
        // Only update if popup is visible
        if (deckPopup == null || deckPopup.style.display != DisplayStyle.Flex)
            return;
        var blackjackManager = FindFirstObjectByType<NetworkBlackjackManager>();
        if (blackjackManager == null) return;
        // Find local player index
        ulong myLocalClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
        int playerIdx = -1;
        for (int i = 0; i < blackjackManager.PlayerIds.Count; i++)
        {
            if (blackjackManager.PlayerIds[i] == myLocalClientId)
            {
                playerIdx = i;
                break;
            }
        }
        if (playerIdx < 0 || playerIdx >= blackjackManager.PlayerIds.Count) return;
        // If host, refresh deck directly
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            var deckField = typeof(NetworkBlackjackManager).GetField("playerDecks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (deckField == null) return;
            var decksObj = deckField.GetValue(blackjackManager) as List<List<CardData>>;
            if (decksObj == null || playerIdx >= decksObj.Count) return;
            var deck = decksObj[playerIdx];
            OnDeckReceivedFromHost(deck);
        }
        else
        {
            // Non-host: re-request deck from host
            blackjackManager.RequestDeckFromHostServerRpc(playerIdx);
        }
    }

    // Handler for deck received from host
    private void OnDeckReceivedFromHost(List<CardData> deck)
    {
        // Set popup style for readability
        deckPopup.style.backgroundColor = new StyleColor(new Color(0.95f, 0.95f, 0.98f, 0.98f));
        deckPopup.style.color = Color.black;
        deckListScroll.style.backgroundColor = new StyleColor(new Color(0.93f, 0.93f, 1f, 0.98f));
        deckListScroll.style.color = Color.black;
        deckListScroll.Clear();
        // Sort deck by value ascending
        var sortedDeck = new List<CardData>(deck);
        sortedDeck.Sort((a, b) => a.value.CompareTo(b.value));
        for (int i = 0; i < sortedDeck.Count; i++)
        {
            var card = sortedDeck[i];
            var cardLabel = new Label($"{CardToString(card.value)} (Type: {card.cardType}, Suit: {card.suit}, PowerId: {card.powerId})");
            cardLabel.style.color = Color.black;
            deckListScroll.Add(cardLabel);
        }
        deckPopup.style.display = DisplayStyle.Flex;
    }

    // Show Deck button click handler
    private void OnShowDeckClicked()
    {
        var blackjackManager = FindFirstObjectByType<NetworkBlackjackManager>();
        if (blackjackManager == null || deckListScroll == null) return;
        // Find local player index
        ulong myLocalClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
        int playerIdx = -1;
        for (int i = 0; i < blackjackManager.PlayerIds.Count; i++)
        {
            if (blackjackManager.PlayerIds[i] == myLocalClientId)
            {
                playerIdx = i;
                break;
            }
        }
        if (playerIdx < 0 || playerIdx >= blackjackManager.PlayerIds.Count) return;
        // Set popup style for readability
        deckPopup.style.backgroundColor = new StyleColor(new Color(0.95f, 0.95f, 0.98f, 0.98f));
        deckPopup.style.color = Color.black;
        deckListScroll.style.backgroundColor = new StyleColor(new Color(0.93f, 0.93f, 1f, 0.98f));
        deckListScroll.style.color = Color.black;

        // If host, get deck directly
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
        {
            var deckField = typeof(NetworkBlackjackManager).GetField("playerDecks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (deckField == null) return;
            var decksObj = deckField.GetValue(blackjackManager) as List<List<CardData>>;
            if (decksObj == null || playerIdx >= decksObj.Count) return;
            var deck = decksObj[playerIdx];
            deckListScroll.Clear();
            for (int i = 0; i < deck.Count; i++)
            {
                var card = deck[i];
                var cardLabel = new Label($"{CardToString(card.value)} (Type: {card.cardType}, Suit: {card.suit}, PowerId: {card.powerId})");
                cardLabel.style.color = Color.black;
                deckListScroll.Add(cardLabel);
            }
            deckPopup.style.display = DisplayStyle.Flex;
        }
        else
        {
            // Non-host: request deck from host
            deckListScroll.Clear();
            var loadingLabel = new Label("Loading deck from host...");
            loadingLabel.style.color = Color.black;
            deckListScroll.Add(loadingLabel);
            deckPopup.style.display = DisplayStyle.Flex;
            blackjackManager.RequestDeckFromHostServerRpc(playerIdx);
        }
    }

    private void OnHitClicked()
    {
        var blackjackManager = FindFirstObjectByType<NetworkBlackjackManager>();
        if (blackjackManager != null)
            blackjackManager.HitServerRpc();
    }

    private void OnStandClicked()
    {
        var blackjackManager = FindFirstObjectByType<NetworkBlackjackManager>();
        if (blackjackManager != null)
            blackjackManager.StandServerRpc();
    }

    private void OnStartRoundClicked()
    {
        var blackjackManager = FindFirstObjectByType<NetworkBlackjackManager>();
        if (blackjackManager != null)
            blackjackManager.StartRoundServerRpc();
    }

    private void UpdateUI()
    {
        // Enable StartRoundButton only if game is over
        var blackjackManager = FindFirstObjectByType<NetworkBlackjackManager>();
        if (startRoundButton != null)
        {
            bool isGameOver = blackjackManager != null && blackjackManager.gameState.Value == NetworkBlackjackManager.GameState.GameOver;
            startRoundButton.SetEnabled(isGameOver);
            startRoundButton.style.display = isGameOver ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Multiplayer info
        string gameStateStr = blackjackManager != null ? blackjackManager.gameState.Value.ToString() : "";

        // Update stats panels
        if (blackjackManager != null && blackjackManager.PlayerIds.Count > 0)
        {
            ulong localClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            int localPlayerIdx = blackjackManager.PlayerIds.IndexOf(localClientId);
            // Local player panel
            if (localPlayerIdx >= 0 && localPlayerIdx < blackjackManager.PlayerHands.Count)
            {
                var hand = blackjackManager.PlayerHands[localPlayerIdx];
                int handValue = GetBlackjackHandValue(hand);
                int hp = (localPlayerIdx < blackjackManager.PlayerHP.Count) ? blackjackManager.PlayerHP[localPlayerIdx] : 0;
                int chips = (localPlayerIdx < blackjackManager.PlayerChips.Count) ? blackjackManager.PlayerChips[localPlayerIdx] : 0;
                if (handValueLabel != null) handValueLabel.text = $"Hand Value: {handValue}";
                if (hpLabel != null) hpLabel.text = $"HP: {hp}";
                if (chipsLabel != null) chipsLabel.text = $"Chips: {chips}";
            }
            // Other player panel (first non-local player)
            int otherIdx = -1;
            for (int i = 0; i < blackjackManager.PlayerIds.Count; i++)
            {
                if (i != localPlayerIdx) { otherIdx = i; break; }
            }
            if (otherIdx >= 0 && otherIdx < blackjackManager.PlayerHands.Count)
            {
                var hand = blackjackManager.PlayerHands[otherIdx];
                int handValue = GetBlackjackHandValue(hand);
                int hp = (otherIdx < blackjackManager.PlayerHP.Count) ? blackjackManager.PlayerHP[otherIdx] : 0;
                int chips = (otherIdx < blackjackManager.PlayerChips.Count) ? blackjackManager.PlayerChips[otherIdx] : 0;
                if (otherHandValueLabel != null) otherHandValueLabel.text = $"Hand Value: {handValue}";
                if (otherHPLabel != null) otherHPLabel.text = $"HP: {hp}";
                if (otherChipsLabel != null) otherChipsLabel.text = $"Chips: {chips}";
            }
        }
        if (blackjackManager != null && infoPanel != null)
        {
            infoPanel.Clear();
            int winnerIdx = -999;
            var winnerField = blackjackManager.GetType().GetField("winnerIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(blackjackManager);
            if (winnerField != null)
            {
                var winnerValueProp = winnerField.GetType().GetProperty("Value");
                if (winnerValueProp != null)
                {
                    var val = winnerValueProp.GetValue(winnerField);
                    if (val is int idx) winnerIdx = idx;
                }
            }

            // Show local player's chip count
            ulong localClientId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            int localPlayerIdx = blackjackManager.PlayerIds.IndexOf(localClientId);
            int chips = (localPlayerIdx >= 0 && localPlayerIdx < blackjackManager.PlayerChips.Count) ? blackjackManager.PlayerChips[localPlayerIdx] : 0;
            if (chipsLabel != null)
                chipsLabel.text = $"Chips: {chips}";

            // Optionally, show chip counts for all players in InfoPanel
            for (int i = 0; i < blackjackManager.PlayerHands.Count; i++)
            {
                int playerChips = (i < blackjackManager.PlayerChips.Count) ? blackjackManager.PlayerChips[i] : 0;
                var chipLabel = new Label($"Player {i + 1} Chips: {playerChips}");
                infoPanel.Add(chipLabel);
            }

            for (int i = 0; i < blackjackManager.PlayerHands.Count; i++)
            {
                unsafe
                {
                    var hand = blackjackManager.PlayerHands[i];
                    string handStr = "";
                    for (int c = 0; c < hand.count; c++)
                    {
                        CardData card = hand.Get(c);
                        handStr += CardToString(card.value) + " ";
                    }
                    int handValue = GetBlackjackHandValue(hand);
                    int hp = (i < blackjackManager.PlayerHP.Count) ? blackjackManager.PlayerHP[i] : 0;
                    string winnerNote = (winnerIdx == i) ? " [WINNER]" : "";
                    string eliminatedNote = (hp <= 0) ? " [ELIMINATED]" : "";
                    var label = new Label($"Player {i + 1} Hand: {handStr} (Value: {handValue}) | HP: {hp}{winnerNote}{eliminatedNote}");
                    label.AddToClassList("info-label");
                    infoPanel.Add(label);
                }
            }

            // Show PvP round result
            if (gameStateStr == "RoundOver")
            {
                string resultMsg = "";
                if (winnerIdx == -1)
                    resultMsg = "All players bust! No winner.";
                else if (winnerIdx == -2)
                    resultMsg = "It's a tie!";
                else if (winnerIdx >= 0 && winnerIdx < blackjackManager.PlayerIds.Count)
                {
                    // Show damage dealt to each player
                    int winnerVal = 0;
                    int winnerCardCount = 0;
                    unsafe
                    {
                        var winnerHand = blackjackManager.PlayerHands[winnerIdx];
                        winnerVal = GetBlackjackHandValue(winnerHand);
                        winnerCardCount = winnerHand.count;
                    }
                    string damageInfo = "";
                    for (int i = 0; i < blackjackManager.PlayerHands.Count; i++)
                    {
                        if (i == winnerIdx) continue;
                        int hp = (i < blackjackManager.PlayerHP.Count) ? blackjackManager.PlayerHP[i] : 0;
                        int loserVal = 0;
                        unsafe
                        {
                            var loserHand = blackjackManager.PlayerHands[i];
                            loserVal = GetBlackjackHandValue(loserHand);
                        }
                        if (loserVal > 21) loserVal = 0;
                        if (hp <= 0) continue;
                        int dmg = winnerVal - loserVal;
                        if (winnerVal == 21 && winnerCardCount == 2) dmg += 5;
                        damageInfo += $"Player {i + 1} took {dmg} damage. ";
                    }
                    resultMsg = $"Player {winnerIdx + 1} wins! {damageInfo}";
                }
                else
                    resultMsg = "";
                var resultLabel = new Label($"[Result] {resultMsg}");
                resultLabel.AddToClassList("info-label");
                infoPanel.Add(resultLabel);
            }
            else if (gameStateStr == "GameOver")
            {
                string winnerMsg = (winnerIdx >= 0 && winnerIdx < blackjackManager.PlayerIds.Count) ? $"Player {winnerIdx + 1} is the last standing!" : "Game Over!";
                var overLabel = new Label(winnerMsg);
                overLabel.AddToClassList("info-label");
                infoPanel.Add(overLabel);
            }
        }
        // Local player hand panel (networked)
        if (blackjackManager == null) return;
        // Use DealerPanel for local player's hand and OtherHandsPanel for others
        // Only declare these ONCE at the top of the method to avoid CS0128 errors
        var root = blackjackUIDocument != null ? blackjackUIDocument.rootVisualElement : null;
        var localHandPanel = root != null ? root.Q<VisualElement>("LocalPlayerHand") : null;
        var otherHandPanel = root != null ? root.Q<VisualElement>("OtherPlayerHand") : null;
        if (localHandPanel != null)
            localHandPanel.Clear();
        if (otherHandPanel != null)
            otherHandPanel.Clear();
        // Find local player index
        var networkManager = Unity.Netcode.NetworkManager.Singleton;
        ulong myLocalClientId = networkManager != null ? networkManager.LocalClientId : 0;
        int playerIdx = -1;
        Debug.Log($"[UI] UpdateUI called. myLocalClientId={myLocalClientId}, playerIds=[{string.Join(",", blackjackManager.PlayerIds)}]");
        for (int i = 0; i < blackjackManager.PlayerIds.Count; i++)
        {
            if (blackjackManager.PlayerIds[i] == myLocalClientId)
            {
                playerIdx = i;
                break;
            }
        }
        Debug.Log($"[UI] Calculated playerIdx={playerIdx}");

        // Show ONLY the local player's hand as card images in LocalPlayerHand
        if (localHandPanel != null && playerIdx >= 0 && playerIdx < blackjackManager.PlayerHands.Count)
        {
            unsafe
            {
                var hand = blackjackManager.PlayerHands[playerIdx];
                for (int c = 0; c < hand.count; c++)
                {
                    CardData card = hand.Get(c);
                    string spritePath = SpawnedCardHelper.CardIntToSpritePath(card.value);
                    var sprite = Resources.Load<Sprite>(spritePath);
                    var img = new Image();
                    img.sprite = sprite;
                    img.style.width = 80;
                    img.style.height = 120;
                    img.AddToClassList("card-image");
                    localHandPanel.Add(img);
                }
            }
        }
        // Show all other players' hands as card images in OtherPlayerHand
        if (otherHandPanel != null)
        {
            otherHandPanel.Clear();
            for (int i = 0; i < blackjackManager.PlayerHands.Count; i++)
            {
                if (i == playerIdx) continue;
                unsafe
                {
                    var hand = blackjackManager.PlayerHands[i];
                    var row = new VisualElement();
                    row.style.flexDirection = FlexDirection.Row;
                    row.AddToClassList("opponent-hand-row");
                    for (int c = 0; c < hand.count; c++)
                    {
                        CardData card = hand.Get(c);
                        string spritePath = SpawnedCardHelper.CardIntToSpritePath(card.value);
                        var sprite = Resources.Load<Sprite>(spritePath);
                        var img = new Image();
                        img.sprite = sprite;
                        img.style.width = 80;
                        img.style.height = 120;
                        img.AddToClassList("card-image");
                        row.Add(img);
                    }
                    otherHandPanel.Add(row);
                }
            }
        }

        // Optionally update playerLabel to just show your hand summary
        if (playerIdx >= 0 && playerIdx < blackjackManager.PlayerHands.Count && playerLabel != null)
        {
            unsafe
            {
                var hand = blackjackManager.PlayerHands[playerIdx];
                int handValue = 0;
                string handStr = "";
                for (int c = 0; c < hand.count; c++)
                {
                    CardData card = hand.Get(c);
                    handStr += CardToString(card.value) + " ";
                }
                handValue = GetBlackjackHandValue(hand);
                int hp = (playerIdx < blackjackManager.PlayerHP.Count) ? blackjackManager.PlayerHP[playerIdx] : 0;
                playerLabel.text = $"Your Hand (Value: {handValue}) | HP: {hp}";
            }
        }
        // Show/hide action buttons
        // Only enable buttons if it's the local player's turn and the game state is PlayerTurn
        bool isPlayerTurn = false;
        Debug.Log($"[UI] UpdateUI: localClientId={Unity.Netcode.NetworkManager.Singleton.LocalClientId}, playerIdx={playerIdx}, CurrentTurnIndex={blackjackManager?.CurrentTurnIndex}, gameStateStr={gameStateStr}");
        if (gameStateStr == "PlayerTurn" && blackjackManager != null && playerIdx == blackjackManager.CurrentTurnIndex)
        {
            isPlayerTurn = true;
        }
        Debug.Log($"[UI] Button State: isPlayerTurn={isPlayerTurn}, hitButton={(hitButton != null ? "found" : "null")}, standButton={(standButton != null ? "found" : "null")}");
        if (hitButton != null) hitButton.SetEnabled(isPlayerTurn);
        if (standButton != null) standButton.SetEnabled(isPlayerTurn);
    }

    private static string CardToString(int card)
    {
        switch (card)
        {
            case 1: return "A";
            case 11: return "J";
            case 12: return "Q";
            case 13: return "K";
            default: return card.ToString();
        }
    }

    private static int GetCardValue(int card)
    {
        // For single card display only (not for hand value)
        if (card == 1) return 1;
        if (card >= 11 && card <= 13) return 10;
        return card;
    }

    private static int GetBlackjackHandValue(PlayerHand hand)
    {
        int value = 0;
        int aces = 0;
        System.Text.StringBuilder handLog = new System.Text.StringBuilder();
        unsafe
        {
            for (int i = 0; i < hand.count; i++)
            {
                CardData card = hand.Get(i);
                handLog.Append(card.value + ",");
                if (card.value == 1) aces++;
                value += (card.value > 10) ? 10 : card.value;
            }
        }
        int initialValue = value;
        int initialAces = aces;
        // Handle ace as 11 if possible
        while (aces > 0 && value <= 11)
        {
            value += 10;
            aces--;
        }
        Debug.Log($"[GetBlackjackHandValue] Hand: [{handLog}] | InitialValue: {initialValue} | InitialAces: {initialAces} | FinalValue: {value}");
        return value;
    }

    // Handler for Join Table button
    private void OnJoinTableClicked()
    {
        var blackjackManager = FindFirstObjectByType<NetworkBlackjackManager>();
        if (blackjackManager != null)
        {
            blackjackManager.JoinTableServerRpc();
        }
    }

    // Handler for Start Session button
    private void OnStartSessionClicked()
    {
        var blackjackManager = FindFirstObjectByType<NetworkBlackjackManager>();
        if (blackjackManager != null)
        {
            blackjackManager.StartRoundServerRpc();
        }
    }
}