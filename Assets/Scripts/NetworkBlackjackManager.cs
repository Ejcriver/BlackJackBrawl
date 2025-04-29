using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class NetworkBlackjackManager : NetworkBehaviour
{
    public enum GameState : byte { Waiting, Dealing, PlayerTurn, RoundOver }

    // Networked state
    private NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(GameState.Waiting);
    private NetworkList<ulong> playerIds;
    private List<int> deck;
    private NetworkList<PlayerHand> playerHands;
    private NetworkVariable<int> currentTurnIndex = new NetworkVariable<int>(0);
    private NetworkVariable<int> winnerIndex = new NetworkVariable<int>(-1); // -1: no winner yet

    public NetworkList<ulong> PlayerIds => playerIds;
    public NetworkList<PlayerHand> PlayerHands => playerHands;
    public int WinnerIndex => winnerIndex.Value; // for UI


    private void Awake()
    {
        playerIds = new NetworkList<ulong>();
        playerHands = new NetworkList<PlayerHand>();
        deck = new List<int>();
    }

    public event System.Action OnPlayerListChanged;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        playerIds.OnListChanged += (change) =>
        {
            OnPlayerListChanged?.Invoke();
        };
    }

    [ServerRpc(RequireOwnership = false)]
    public void JoinTableServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (!playerIds.Contains(clientId))
        {
            playerIds.Add(clientId);
            playerHands.Add(new PlayerHand { count = 0 });
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
        if (playerIndex != currentTurnIndex.Value || gameState.Value != GameState.PlayerTurn)
            return; // Not this player's turn
        var hand = playerHands[playerIndex];
        hand.Add(DrawCard());
        playerHands[playerIndex] = hand;
        // After hit, advance turn (even if bust)
        NextTurn();
    }

    // Player requests to stand
    [ServerRpc(RequireOwnership = false)]
    public void StandServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong clientId = rpcParams.Receive.SenderClientId;
        int playerIndex = playerIds.IndexOf(clientId);
        if (playerIndex != currentTurnIndex.Value || gameState.Value != GameState.PlayerTurn)
            return; // Not this player's turn
        // After stand, advance turn
        NextTurn();
    }

    // Advance turn or start dealer
    private void NextTurn()
    {
        currentTurnIndex.Value++;
        if (currentTurnIndex.Value >= playerHands.Count)
        {
            // All players have acted, determine winner
            int maxValue = -1;
            int maxIndex = -1;
            bool tie = false;
            for (int i = 0; i < playerHands.Count; i++)
            {
                int v = HandValue(playerHands[i]);
                if (v > 21) continue; // busts can't win
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
            if (maxValue == -1) // all bust
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
            }
            gameState.Value = GameState.RoundOver;
        }
    }
}
