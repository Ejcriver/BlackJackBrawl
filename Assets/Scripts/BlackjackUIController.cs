using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class BlackjackUIController : MonoBehaviour
{
    public UIDocument blackjackUIDocument;
    public BlackjackGameLogic blackjackGameLogic;

    private Button hitButton;
    private Button standButton;
    private VisualElement playerHandPanel;
    private Label messageLabel;
    private Label playerLabel; // Remove dealerLabel

    void Awake()
    {
        if (blackjackUIDocument == null)
            blackjackUIDocument = GetComponent<UIDocument>();
        if (blackjackGameLogic == null)
            blackjackGameLogic = FindFirstObjectByType<BlackjackGameLogic>();
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
            messageLabel = root.Q<Label>("MessageLabel");
            playerLabel = root.Q<Label>("PlayerLabel");

            if (hitButton != null)
                hitButton.clicked += OnHitClicked;
            if (standButton != null)
                standButton.clicked += OnStandClicked;

            if (blackjackGameLogic != null)
                blackjackGameLogic.onGameStateChanged += UpdateUI;

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
        if (blackjackGameLogic != null)
            blackjackGameLogic.onGameStateChanged -= UpdateUI;
    }

    private void OnHitClicked()
    {
        if (blackjackGameLogic != null)
            blackjackGameLogic.PlayerHit();
    }

    private void OnStandClicked()
    {
        if (blackjackGameLogic != null)
            blackjackGameLogic.PlayerStand();
    }

    private void UpdateUI()
    {
        if (blackjackGameLogic == null) return;
        // Update player hand
        playerHandPanel.Clear();
        foreach (int card in blackjackGameLogic.playerHand)
        {
            var cardLabel = new Label(CardToString(card));
            cardLabel.AddToClassList("info-label");
            playerHandPanel.Add(cardLabel);
        }
        // Update hand values
        if (playerLabel != null)
            playerLabel.text = $"Your Hand ({blackjackGameLogic.HandValue(blackjackGameLogic.playerHand)})";
        // Show result if round is over
        if (blackjackGameLogic.gameState == BlackjackGameLogic.GameState.RoundOver)
        {
            if (messageLabel != null)
                messageLabel.text = blackjackGameLogic.GetResult(); // This should be updated to show PvP winner
            if (hitButton != null) hitButton.SetEnabled(false);
            if (standButton != null) hitButton.SetEnabled(false);
        }
        else
        {
            if (messageLabel != null)
                messageLabel.text = "";
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
                    string winnerNote = (winnerIdx == i) ? " [WINNER]" : "";
                    GUILayout.Label($"Player {i + 1} Hand: {handStr} (Value: {handValue}){winnerNote}");
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
                    resultMsg = $"Player {winnerIdx + 1} wins!";
                else
                    resultMsg = "";
                GUILayout.Label($"[Result] {resultMsg}");
            }

        }
        GUILayout.EndArea();
    }
}
