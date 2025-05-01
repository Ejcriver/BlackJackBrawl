using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class NetworkBlackjackManager : NetworkBehaviour
{
    public enum GameState : byte { Waiting, Dealing, PlayerTurn, RoundOver, GameOver }

    // Networked state
    public NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(GameState.Waiting);
    private NetworkList<ulong> playerIds;
    private List<int> deck;
    private NetworkList<PlayerHand> playerHands;
    private NetworkVariable<int> currentTurnIndex = new NetworkVariable<int>(0);
    public int CurrentTurnIndex => currentTurnIndex.Value;
    public NetworkVariable<int> winnerIndex = new NetworkVariable<int>(-1); // -1: no winner yet
    private NetworkList<int> playerHP; // HP for each player
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
        playerActions = new NetworkList<byte>();
        deck = new List<int>();
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
            playerActions.Add((byte)PlayerActionState.None);
            playerChips.Add(0); // Start with 0 chips (or set to desired initial amount)
            Debug.Log($"[JoinTableServerRpc] Added clientId={clientId}. playerIds now: [{string.Join(",", playerIds)}]");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartRoundServerRpc()
    {
        if (!IsServer) return;
        // Reset deck and player hands
        deck = CreateDeck();
        for (int i = 0; i < playerHands.Count; i++)
        {
            var hand = playerHands[i];
            hand.Clear();
            playerHands[i] = hand;
        }
        // Deal two cards to each player
        for (int i = 0; i < playerHands.Count; i++)
        {
            var hand = playerHands[i];
            hand.Add(DrawCard());
            hand.Add(DrawCard());
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

    private List<int> CreateDeck()
    {
        var newDeck = new List<int>(52);
        // Add four of each card value (1-13)
        for (int value = 1; value <= 13; value++)
        {
            for (int suit = 0; suit < 4; suit++)
            {
                newDeck.Add(value);
            }
        }
        // Shuffle deck using Fisher-Yates
        for (int i = newDeck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = newDeck[i];
            newDeck[i] = newDeck[j];
            newDeck[j] = temp;
        }
        return newDeck;
    }

    private int DrawCard()
    {
        if (deck.Count == 0) deck = CreateDeck();
        int idx = Random.Range(0, deck.Count);
        int card = deck[idx];
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
                int v = hand.cards[i];
                if (v == 1) aces++;
                value += v > 10 ? 10 : v;
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
        hand.Add(DrawCard());
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
        }
    }

    private System.Collections.IEnumerator NextRoundDelayCoroutine()
    {
        yield return new UnityEngine.WaitForSeconds(2f);
        StartRoundServerRpc();
    }

    public enum PlayerActionState : byte { None, Stand, Bust }
}
