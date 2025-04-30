using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class BlackjackUIController : MonoBehaviour
{
    public UIDocument blackjackUIDocument;
    // public BlackjackGameLogic blackjackGameLogic; // No longer used

    private Button hitButton;
    private Button standButton;
    private VisualElement playerHandPanel;
    private VisualElement infoPanel;
    private Label messageLabel;
    private Label playerLabel; // Remove dealerLabel
    private List<Label> playerInfoLabels = new List<Label>();

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

            if (hitButton != null)
                hitButton.clicked += OnHitClicked;
            if (standButton != null)
                standButton.clicked += OnStandClicked;

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

    private void UpdateUI()
    {
        // Multiplayer info
        var blackjackManager = FindFirstObjectByType<NetworkBlackjackManager>();
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
            for (int i = 0; i < blackjackManager.PlayerHands.Count; i++)
            {
                unsafe
                {
                    var hand = blackjackManager.PlayerHands[i];
                    string handStr = "";
                    int handValue = 0;
                    for (int c = 0; c < hand.count; c++)
                    {
                        int card = hand.cards[c];
                        handStr += CardToString(card) + " ";
                        handValue += GetCardValue(card);
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
                        for (int c = 0; c < winnerHand.count; c++) winnerVal += GetCardValue(winnerHand.cards[c]);
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
                            for (int c = 0; c < loserHand.count; c++) loserVal += GetCardValue(loserHand.cards[c]);
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
        playerHandPanel.Clear();
        // Find local player index
        var networkManager = Unity.Netcode.NetworkManager.Singleton;
        ulong localClientId = networkManager != null ? networkManager.LocalClientId : 0;
        int playerIdx = -1;
        Debug.Log($"[UI] UpdateUI called. localClientId={localClientId}, playerIds=[{string.Join(",", blackjackManager.PlayerIds)}]");
        for (int i = 0; i < blackjackManager.PlayerIds.Count; i++)
        {
            if (blackjackManager.PlayerIds[i] == localClientId)
            {
                playerIdx = i;
                break;
            }
        }
        Debug.Log($"[UI] Calculated playerIdx={playerIdx}");
        if (playerIdx >= 0 && playerIdx < blackjackManager.PlayerHands.Count)
        {
            unsafe
            {
                var hand = blackjackManager.PlayerHands[playerIdx];
                int handValue = 0;
                for (int c = 0; c < hand.count; c++)
                {
                    int card = hand.cards[c];
                    var cardLabel = new Label(CardToString(card));
                    cardLabel.AddToClassList("info-label");
                    playerHandPanel.Add(cardLabel);
                    handValue += GetCardValue(card);
                }
                int hp = (playerIdx < blackjackManager.PlayerHP.Count) ? blackjackManager.PlayerHP[playerIdx] : 0;
                if (playerLabel != null)
                    playerLabel.text = $"Your Hand (Value: {handValue}) | HP: {hp}";
            }
        }
        // Show/hide action buttons
        // Use the already declared gameStateStr
        if (gameStateStr == "RoundOver" || gameStateStr == "GameOver")
        {
            if (hitButton != null) hitButton.SetEnabled(false);
            if (standButton != null) hitButton.SetEnabled(false);
        }
        else
        {
            if (hitButton != null) hitButton.SetEnabled(true);
            if (standButton != null) standButton.SetEnabled(true);
        }
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
                        int card = hand.cards[c];
                        handStr += CardToString(card) + " ";
                        handValue += GetCardValue(card);
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
                        for (int c = 0; c < winnerHand.count; c++) winnerVal += GetCardValue(winnerHand.cards[c]);
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
                            for (int c = 0; c < loserHand.count; c++) loserVal += GetCardValue(loserHand.cards[c]);
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
