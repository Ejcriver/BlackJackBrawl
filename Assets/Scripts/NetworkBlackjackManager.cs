using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class NetworkBlackjackManager : NetworkBehaviour
{
    public enum GameState : byte { Waiting, Dealing, PlayerTurn, DealerTurn, RoundOver }

    // Networked state
    private NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(GameState.Waiting);
    private NetworkList<ulong> playerIds;
    private List<int> deck;
    private NetworkList<int> dealerHand;
    public NetworkList<int> DealerHand => dealerHand;
    private NetworkList<PlayerHand> playerHands;
    private NetworkVariable<int> currentTurnIndex = new NetworkVariable<int>(0);

    public NetworkList<ulong> PlayerIds => playerIds;
    public NetworkList<PlayerHand> PlayerHands => playerHands;

    private void Awake()
    {
        playerIds = new NetworkList<ulong>();
        dealerHand = new NetworkList<int>();
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
        // Reset deck, dealer hand, and player hands
        deck = CreateDeck();
        dealerHand.Clear();
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
        dealerHand.Add(DrawCard());
        dealerHand.Add(DrawCard());
        gameState.Value = GameState.PlayerTurn;
        currentTurnIndex.Value = 0;
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
        if (HandValue(hand) > 21)
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
        if (playerIndex != currentTurnIndex.Value || gameState.Value != GameState.PlayerTurn)
            return; // Not this player's turn
        NextTurn();
    }

    // Advance turn or start dealer
    private void NextTurn()
    {
        currentTurnIndex.Value++;
        if (currentTurnIndex.Value >= playerHands.Count)
        {
            gameState.Value = GameState.DealerTurn;
            DealerPlay();
        }
    }

    // Dealer plays automatically
    private void DealerPlay()
    {
        while (HandValueDealer() < 17)
        {
            dealerHand.Add(DrawCard());
        }
        gameState.Value = GameState.RoundOver;
    }

    private int HandValueDealer()
    {
        int value = 0;
        int aces = 0;
        for (int i = 0; i < dealerHand.Count; i++)
        {
            int v = dealerHand[i];
            if (v == 1) aces++;
            value += v > 10 ? 10 : v;
        }
        while (aces > 0 && value <= 11)
        {
            value += 10;
            aces--;
        }
        return value;
    }
}
