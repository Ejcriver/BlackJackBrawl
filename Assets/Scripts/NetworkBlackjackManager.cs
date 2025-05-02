using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class NetworkBlackjackManager : NetworkBehaviour
{
    // For deck popup sync
    public static System.Action<List<CardData>> OnDeckReceivedFromHost;

    public enum GameState : byte { Waiting, Dealing, PlayerTurn, RoundOver, GameOver }

    // Networked state
    public NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(GameState.Waiting);
    private NetworkList<ulong> playerIds;
    // Per-player decks for custom cards and power cards
    private List<List<CardData>> playerDecks = new List<List<CardData>>(); // All player decks now use CardData
    private NetworkList<PlayerHand> playerHands;
    private NetworkVariable<int> currentTurnIndex = new NetworkVariable<int>(0);
    public int CurrentTurnIndex => currentTurnIndex.Value;
    public NetworkVariable<int> winnerIndex = new NetworkVariable<int>(-1); // -1: no winner yet
    private NetworkList<int> playerHP; // HP for each player
    private NetworkList<int> playerMaxHP; // Max HP for each player
    private NetworkList<byte> playerActions; // Track stand/bust as byte
    private NetworkList<int> playerChips; // Chips for each player

    public NetworkList<ulong> PlayerIds => playerIds;


    public NetworkList<PlayerHand> PlayerHands => playerHands;
    public NetworkList<int> PlayerHP => playerHP;
    public NetworkList<int> PlayerChips => playerChips;
    public int WinnerIndex => winnerIndex.Value; // for UI


    private void Awake()
    {
        playerIds = new NetworkList<ulong>();
        playerHands = new NetworkList<PlayerHand>();
        playerHP = new NetworkList<int>();
        playerMaxHP = new NetworkList<int>();
        playerActions = new NetworkList<byte>();
        playerDecks = new List<List<CardData>>();
        playerChips = new NetworkList<int>();
    }

    public event System.Action OnPlayerListChanged;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        playerIds.OnListChanged += (change) =>
        {
            Debug.Log($"[Network] playerIds changed: [{string.Join(",", playerIds)}]");
            OnPlayerListChanged?.Invoke();
        };
        playerHands.OnListChanged += (change) =>
        {
            Debug.Log($"[Network] playerHands changed");
            OnPlayerListChanged?.Invoke();
        };
        playerHP.OnListChanged += (change) =>
        {
            Debug.Log($"[Network] playerHP changed: [{string.Join(",", playerHP)}]");
            OnPlayerListChanged?.Invoke();
        };
        playerActions.OnListChanged += (change) =>
        {
            Debug.Log($"[Network] playerActions changed: [{string.Join(",", playerActions)}]");
            OnPlayerListChanged?.Invoke();
        };
        playerChips.OnListChanged += (change) =>
        {
            Debug.Log($"[Network] playerChips changed: [{string.Join(",", playerChips)}]");
            OnPlayerListChanged?.Invoke();
        };
        // Ensure UI updates on host and clients when game state or turn changes
        gameState.OnValueChanged += (oldVal, newVal) => { OnPlayerListChanged?.Invoke(); };
        currentTurnIndex.OnValueChanged += (oldVal, newVal) => { OnPlayerListChanged?.Invoke(); };
    }

    [ServerRpc(RequireOwnership = false)]
    public void JoinTableServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"[JoinTableServerRpc] clientId={clientId}, playerIds=[{string.Join(",", playerIds)}]");
        if (!playerIds.Contains(clientId))
        {
            playerIds.Add(clientId);
            playerHands.Add(new PlayerHand { count = 0 });
            playerHP.Add(30); // Start with 30 HP
            playerMaxHP.Add(30); // Start with 30 Max HP
            playerActions.Add((byte)PlayerActionState.None);
            playerChips.Add(0); // Start with 0 chips (or set to desired initial amount)
            // Create a new deck for this player (1–13, one of each card)
            var playerDeck = new List<CardData>();
            for (int value = 1; value <= 13; value++)
                playerDeck.Add(new CardData { cardType = CardType.Standard, value = value, suit = 0, powerId = 0 });
            Shuffle(playerDeck);
            playerDecks.Add(playerDeck);
            Debug.Log($"[JoinTableServerRpc] Added clientId={clientId}. playerIds now: [{string.Join(",", playerIds)}]");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartRoundServerRpc()
    {
        if (!IsServer) return;
        // Reset all player decks and hands
        for (int i = 0; i < playerDecks.Count; i++)
        {
            // Preserve power cards, only reset standard cards
            var powerCards = playerDecks[i].FindAll(card => card.cardType == CardType.Power);
            playerDecks[i].Clear();
            for (int value = 1; value <= 13; value++)
                playerDecks[i].Add(new CardData { cardType = CardType.Standard, value = value, suit = 0, powerId = 0 });
            playerDecks[i].AddRange(powerCards);
            Shuffle(playerDecks[i]);
        }
        for (int i = 0; i < playerHands.Count; i++)
        {
            var hand = playerHands[i];
            hand.Clear();
            playerHands[i] = hand;
        }
        // Deal two cards to each player from their own deck
        for (int i = 0; i < playerHands.Count; i++)
        {
            var hand = playerHands[i];
            hand.Add(DrawCard(i));
            hand.Add(DrawCard(i));
            playerHands[i] = hand;
        }
        // Reset player action states
        playerActions.Clear();
        for (int i = 0; i < playerHands.Count; i++)
            playerActions.Add((byte)PlayerActionState.None);
        // Optionally, reset chips at round start (commented out for persistent chips)
        // for (int i = 0; i < playerChips.Count; i++) playerChips[i] = 0;
        gameState.Value = GameState.PlayerTurn;
        currentTurnIndex.Value = 0;
        winnerIndex.Value = -1;
    }

    // Shuffle helper for per-player decks
    private void Shuffle(List<CardData> deck)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            CardData temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
    }

    // Draw a card from a player's deck (by player index)
    private CardData DrawCard(int playerIdx)
    {
        if (playerIdx < 0 || playerIdx >= playerDecks.Count)
            return default;
        var deck = playerDecks[playerIdx];
        if (deck.Count == 0)
        {
            // Optionally: reshuffle discard pile or prevent draw
            // For now, just refill with a new 1–13 deck
            for (int value = 1; value <= 13; value++)
                deck.Add(new CardData { cardType = CardType.Standard, value = value, suit = 0, powerId = 0 });
            Shuffle(deck);
        }
        int idx = Random.Range(0, deck.Count);
        CardData card = deck[idx];
        deck.RemoveAt(idx);
        return card;
    }

    // Hand value calculation for a PlayerHand
    private int HandValue(PlayerHand hand)
    {
        int value = 0;
        int aces = 0;
        unsafe
        {
            for (int i = 0; i < hand.count; i++)
            {
                CardData card = hand.Get(i);
                if (card.value == 1) aces++;
                value += card.value > 10 ? 10 : card.value;
            }
        }
        // Handle ace as 11 if possible
        while (aces > 0 && value <= 11)
        {
            value += 10;
            aces--;
        }
        return value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void BuyMaxHPServerRpc(ulong clientId)
    {
        int playerIndex = playerIds.IndexOf(clientId);
        if (playerIndex >= 0 && playerIndex < playerChips.Count && playerIndex < playerHP.Count && playerIndex < playerMaxHP.Count)
        {
            if (playerChips[playerIndex] >= 10)
            {
                playerChips[playerIndex] -= 10;
                playerMaxHP[playerIndex] += 5;
                playerHP[playerIndex] = playerMaxHP[playerIndex]; // Optionally heal to new max
                Debug.Log($"[Shop] Player {clientId} bought +5 Max HP. Chips left: {playerChips[playerIndex]}, Max HP: {playerMaxHP[playerIndex]}, HP: {playerHP[playerIndex]}");
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void BuyPowerCardServerRpc(ulong clientId, int powerId)
    {
        int playerIndex = playerIds.IndexOf(clientId);
        if (playerIndex >= 0 && playerIndex < playerChips.Count && playerIndex < playerDecks.Count)
        {
            if (playerChips[playerIndex] >= 15)
            {
                playerChips[playerIndex] -= 15;
                // Add power card to deck
                var card = new CardData { cardType = CardType.Power, value = 0, suit = 0, powerId = powerId };
                playerDecks[playerIndex].Add(card);
                Debug.Log($"[Shop] Player {clientId} bought Power Card (powerId={powerId}). Chips left: {playerChips[playerIndex]}");
            }
            else
            {
                Debug.Log($"[Shop] Player {clientId} tried to buy power card (powerId={powerId}) but didn't have enough chips.");
            }
        }
    }
    // Player requests to hit
    [ServerRpc(RequireOwnership = false)]
    public void HitServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong clientId = rpcParams.Receive.SenderClientId;
        int playerIndex = playerIds.IndexOf(clientId);
        Debug.Log($"[HitServerRpc] clientId={clientId}, playerIndex={playerIndex}, currentTurnIndex={currentTurnIndex.Value}, playerIds=[{string.Join(",", playerIds)}]");
        if (playerIndex != currentTurnIndex.Value || gameState.Value != GameState.PlayerTurn)
        {
            Debug.Log($"[HitServerRpc] Not this player's turn or wrong state.");
            return; // Not this player's turn
        }
        var hand = playerHands[playerIndex];
        CardData card = DrawCard(playerIndex);
        hand.Add(card);
        playerHands[playerIndex] = hand;

        // If bust, mark action as Bust and resolve round
        if (HandValue(hand) > 21)
        {
            playerActions[playerIndex] = (byte)PlayerActionState.Bust;
            Debug.Log($"[HitServerRpc] Player {playerIndex} busted!");
            ResolveRound();
        }
        else
        {
            NextTurn();
        }
    }

    // Player requests to stand
    [ServerRpc(RequireOwnership = false)]
    public void StandServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong clientId = rpcParams.Receive.SenderClientId;
        int playerIndex = playerIds.IndexOf(clientId);
        Debug.Log($"[StandServerRpc] clientId={clientId}, playerIndex={playerIndex}, currentTurnIndex={currentTurnIndex.Value}, playerIds=[{string.Join(",", playerIds)}]");
        if (playerIndex != currentTurnIndex.Value || gameState.Value != GameState.PlayerTurn)
        {
            Debug.Log($"[StandServerRpc] Not this player's turn or wrong state.");
            return; // Not this player's turn
        }
        playerActions[playerIndex] = (byte)PlayerActionState.Stand;
        // If both players have stood, resolve round
        if (AllPlayersStoodOrBusted())
            ResolveRound();
        else
            NextTurn();
    }

    // Advance turn or start dealer
    private void NextTurn()
    {
        currentTurnIndex.Value++;
        if (currentTurnIndex.Value >= playerHands.Count)
        {
            // Loop back to first player, but skip eliminated/busted/standing players
            for (int i = 0; i < playerHands.Count; i++)
            {
                int idx = (currentTurnIndex.Value + i) % playerHands.Count;
                if (playerHP[idx] > 0 && (PlayerActionState)playerActions[idx] == PlayerActionState.None)
                {
                    currentTurnIndex.Value = idx;
                    return;
                }
            }
            // If no eligible players, do nothing (should only happen if all bust/stand)
        }
    }

    private bool AllPlayersStoodOrBusted()
    {
        for (int i = 0; i < playerActions.Count; i++)
        {
            if (playerHP[i] > 0 && (PlayerActionState)playerActions[i] == PlayerActionState.None)
                return false;
        }
        return true;
    }

    private void ResolveRound()
    {
        // Determine winner and deal HP damage
        int maxValue = -1;
        int maxIndex = -1;
        bool tie = false;
        for (int i = 0; i < playerHands.Count; i++)
        {
            int v = HandValue(playerHands[i]);
            if (v > 21 || playerHP[i] <= 0) continue; // busts and eliminated can't win
            if (v > maxValue)
            {
                maxValue = v;
                maxIndex = i;
                tie = false;
            }
            else if (v == maxValue)
            {
                tie = true;
            }
        }
        if (maxValue == -1) // all bust or eliminated
        {
            winnerIndex.Value = -1;
        }
        else if (tie)
        {
            winnerIndex.Value = -2; // -2 means tie
        }
        else
        {
            winnerIndex.Value = maxIndex;
            // Award chips to winner
            int chipsAwarded = 10; // Set chip reward per win here
            if (maxIndex >= 0 && maxIndex < playerChips.Count)
            {
                playerChips[maxIndex] = playerChips[maxIndex] + chipsAwarded;
                Debug.Log($"[Chips] Player {maxIndex} awarded {chipsAwarded} chips. Total: {playerChips[maxIndex]}");
            }
            // Deal damage to all other non-busted, non-eliminated players
            for (int i = 0; i < playerHands.Count; i++)
            {
                if (i == maxIndex || playerHP[i] <= 0) continue;
                int loserValue = HandValue(playerHands[i]);
                if (loserValue > 21) loserValue = 0; // busts are treated as 0 for damage
                int damage = maxValue - loserValue;
                // Blackjack bonus: winner has 21 with 2 cards
                var winnerHand = playerHands[maxIndex];
                int winnerCardCount = winnerHand.count;
                if (maxValue == 21 && winnerCardCount == 2)
                    damage += 5;
                playerHP[i] = Mathf.Max(0, playerHP[i] - damage);
            }
        }
        gameState.Value = GameState.RoundOver;
        // Check if only one player remains with HP > 0
        int alive = 0;
        int lastAliveIdx = -1;
        for (int i = 0; i < playerHP.Count; i++)
        {
            if (playerHP[i] > 0)
            {
                alive++;
                lastAliveIdx = i;
            }
        }
        if (alive > 1)
        {
            // Start next round after delay
            StartCoroutine(NextRoundDelayCoroutine());
        }
        else
        {
            gameState.Value = GameState.GameOver;
            winnerIndex.Value = lastAliveIdx;
            // Reset all players' HP to their Max HP at GameOver
            for (int i = 0; i < playerHP.Count && i < playerMaxHP.Count; i++)
            {
                playerHP[i] = playerMaxHP[i];
            }
        }
    }

    private System.Collections.IEnumerator NextRoundDelayCoroutine()
    {
        yield return new UnityEngine.WaitForSeconds(2f);
        StartRoundServerRpc();
    }

    public enum PlayerActionState : byte { None, Stand, Bust }


    [ServerRpc(RequireOwnership = false)]
    public void RequestDeckFromHostServerRpc(int playerIdx, ServerRpcParams rpcParams = default)
    {
        // Only host/server executes this
        if (!IsServer) return;
        if (playerIdx < 0 || playerIdx >= playerDecks.Count) return;
        var deck = playerDecks[playerIdx];
        // Serialize deck to array for network
        CardData[] deckArr = deck.ToArray();
        ulong clientId = rpcParams.Receive.SenderClientId;
        ReceiveDeckClientRpc(deckArr, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } } });
    }

    [ClientRpc]
    public void ReceiveDeckClientRpc(CardData[] deckArr, ClientRpcParams clientRpcParams = default)
    {
        // Only non-hosts care
        if (OnDeckReceivedFromHost != null)
        {
            OnDeckReceivedFromHost(new List<CardData>(deckArr));
        }
    }
}
