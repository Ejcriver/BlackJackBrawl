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
    private VisualElement playerHandPanel;
    private VisualElement infoPanel;
    private Label messageLabel;
    private Label playerLabel; // Remove dealerLabel
    private Label chipsLabel;
    private List<Label> playerInfoLabels = new List<Label>();

    // Deck UI
    private Button showDeckButton;
    private VisualElement deckPopup;
    private ScrollView deckListScroll;
    private Button closeDeckPopupButton;


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
            startRoundButton = root.Q<Button>("StartRoundButton");
            if (startRoundButton != null)
                startRoundButton.clicked += OnStartRoundClicked;

            if (hitButton != null)
                hitButton.clicked += OnHitClicked;
            if (standButton != null)
                standButton.clicked += OnStandClicked;

            // Add Show Deck button
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

            // Subscribe to deck event for non-hosts
            NetworkBlackjackManager.OnDeckReceivedFromHost += OnDeckReceivedFromHost;

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
        if (showDeckButton != null)
            showDeckButton.clicked -= OnShowDeckClicked;
        NetworkBlackjackManager.OnDeckReceivedFromHost -= OnDeckReceivedFromHost;
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
        for (int i = 0; i < deck.Count; i++)
        {
            var card = deck[i];
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
                    int handValue = 0;
                    for (int c = 0; c < hand.count; c++)
                    {
                        CardData card = hand.Get(c);
                        handStr += CardToString(card.value) + " ";
                        handValue += GetCardValue(card.value);
                    }
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
                        for (int c = 0; c < winnerHand.count; c++) winnerVal += GetCardValue(winnerHand.Get(c).value);
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
                            for (int c = 0; c < loserHand.count; c++) loserVal += GetCardValue(loserHand.Get(c).value);
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
                    handValue += GetCardValue(card.value);
                }
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
        // Blackjack: Ace is 1, J/Q/K are 10, others are face value
        if (card == 1) return 1;
        if (card >= 11 && card <= 13) return 10;
        return card;
    }

    // Multiplayer debug UI for development
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 320, 400), GUI.skin.box);
        GUILayout.Label("Multiplayer Blackjack Controls");

        if (Unity.Netcode.NetworkManager.Singleton != null && !Unity.Netcode.NetworkManager.Singleton.IsClient && !Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Start Host"))
                Unity.Netcode.NetworkManager.Singleton.StartHost();
            if (GUILayout.Button("Start Client"))
                Unity.Netcode.NetworkManager.Singleton.StartClient();
        }

        var blackjackManager = FindFirstObjectByType<NetworkBlackjackManager>();
        if (Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsClient && blackjackManager != null)
        {
            if (GUILayout.Button("Join Table"))
                blackjackManager.JoinTableServerRpc();
            if (GUILayout.Button("Start Round (Host Only)"))
                blackjackManager.StartRoundServerRpc();

            GUILayout.Space(10);
            GUILayout.Label($"Players: {blackjackManager.PlayerIds.Count}");
            var localId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
            GUILayout.Label($"[Debug] LocalClientId: {localId}");
            var turnVar = blackjackManager.GetType().GetField("currentTurnIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(blackjackManager);
            int turnIdx = turnVar is Unity.Netcode.NetworkVariable<int> nv ? nv.Value : 0;
            GUILayout.Label($"[Debug] currentTurnIndex: {turnIdx}");
            // Print PlayerIds as values
            string playerIdsStr = "";
            for (int i = 0; i < blackjackManager.PlayerIds.Count; i++)
            {
                playerIdsStr += blackjackManager.PlayerIds[i];
                if (i < blackjackManager.PlayerIds.Count - 1) playerIdsStr += ", ";
            }
            GUILayout.Label($"[Debug] PlayerIds: [{playerIdsStr}]");
            var gameStateVar = blackjackManager.GetType().GetField("gameState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(blackjackManager);
            string gameStateStr = "";
            if (gameStateVar != null)
            {
                var valueProp = gameStateVar.GetType().GetProperty("Value");
                if (valueProp != null)
                {
                    var enumVal = valueProp.GetValue(gameStateVar);
                    gameStateStr = enumVal != null ? enumVal.ToString() : "";
                    GUILayout.Label($"[Debug] gameStateObj type: {enumVal?.GetType()} value: {gameStateStr}");
                }
                else
                {
                    GUILayout.Label("[Debug] gameStateObj: Value property not found");
                }
            }
            else
            {
                GUILayout.Label("[Debug] gameStateVar is null");
            }

            // Show whose turn (use debug turnIdx from above)
            if (blackjackManager.PlayerIds.Count > 0 && turnIdx < blackjackManager.PlayerIds.Count)
            {
                ulong turnPlayerId = blackjackManager.PlayerIds[turnIdx];
                GUILayout.Label($"Current Turn: Player {turnIdx + 1} (ClientId: {turnPlayerId})");
                GUILayout.Label($"[Debug] LocalClientId == turnPlayerId: {Unity.Netcode.NetworkManager.Singleton.LocalClientId == turnPlayerId}");
                if (Unity.Netcode.NetworkManager.Singleton.LocalClientId == turnPlayerId && 
                    gameStateStr == "PlayerTurn")
                {
                    GUILayout.Label("[Debug] Should see Hit/Stand buttons!");
                    if (GUILayout.Button("Hit"))
                        blackjackManager.HitServerRpc();
                    if (GUILayout.Button("Stand"))
                        blackjackManager.StandServerRpc();
                }
                else
                {
                    GUILayout.Label($"[Debug] Button conditions: LocalClientId={Unity.Netcode.NetworkManager.Singleton.LocalClientId}, turnPlayerId={turnPlayerId}, gameStateStr={gameStateStr}");
                }
            }

            // Show all player hands
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
            for (int i = 0; i < blackjackManager.PlayerHands.Count; i++)
            {
                unsafe
                {
                    var hand = blackjackManager.PlayerHands[i];
                    string handStr = "";
                    int handValue = 0;
                    for (int c = 0; c < hand.count; c++)
                    {
                        CardData card = hand.Get(c);
                        handStr += CardToString(card.value) + " ";
                        handValue += GetCardValue(card.value);
                    }
                    int hp = (i < blackjackManager.PlayerHP.Count) ? blackjackManager.PlayerHP[i] : 0;
                    string winnerNote = (winnerIdx == i) ? " [WINNER]" : "";
                    string eliminatedNote = (hp <= 0) ? " [ELIMINATED]" : "";
                    GUILayout.Label($"Player {i + 1} Hand: {handStr} (Value: {handValue}) | HP: {hp}{winnerNote}{eliminatedNote}");
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
                        for (int c = 0; c < winnerHand.count; c++) winnerVal += GetCardValue(winnerHand.Get(c).value);
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
                            for (int c = 0; c < loserHand.count; c++) loserVal += GetCardValue(loserHand.Get(c).value);
                        }
                        if (loserVal > 21 || hp <= 0) continue;
                        int dmg = winnerVal - loserVal;
                        if (winnerVal == 21 && winnerCardCount == 2) dmg += 5;
                        damageInfo += $"Player {i + 1} took {dmg} damage. ";
                    }
                    resultMsg = $"Player {winnerIdx + 1} wins! {damageInfo}";
                }
                else
                    resultMsg = "";
                GUILayout.Label($"[Result] {resultMsg}");
            }

        }
        GUILayout.EndArea();
    }
}
