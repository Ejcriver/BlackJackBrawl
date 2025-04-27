using System.Collections.Generic;
using UnityEngine;

public class BlackjackGameLogic : MonoBehaviour
{
    public enum GameState { PlayerTurn, DealerTurn, RoundOver }

    public List<int> playerHand = new List<int>();
    public List<int> dealerHand = new List<int>();
    private List<int> deck = new List<int>();
    public GameState gameState = GameState.PlayerTurn;

    public delegate void OnGameStateChanged();
    public event OnGameStateChanged onGameStateChanged;

    void Start()
    {
        StartNewRound();
    }

    public void StartNewRound()
    {
        deck = CreateDeck();
        playerHand.Clear();
        dealerHand.Clear();
        gameState = GameState.PlayerTurn;
        // Initial deal
        playerHand.Add(DrawCard());
        dealerHand.Add(DrawCard());
        playerHand.Add(DrawCard());
        dealerHand.Add(DrawCard());
        onGameStateChanged?.Invoke();
    }

    private List<int> CreateDeck()
    {
        // 1-13: Ace-King (no suits for simplicity)
        List<int> newDeck = new List<int>();
        for (int i = 1; i <= 13; i++)
        {
            for (int j = 0; j < 4; j++) // 4 suits
                newDeck.Add(i);
        }
        Shuffle(newDeck);
        return newDeck;
    }

    private void Shuffle(List<int> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int rnd = Random.Range(i, list.Count);
            int temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }

    private int DrawCard()
    {
        if (deck.Count == 0) deck = CreateDeck();
        int card = deck[0];
        deck.RemoveAt(0);
        return card;
    }

    public void PlayerHit()
    {
        if (gameState != GameState.PlayerTurn) return;
        playerHand.Add(DrawCard());
        if (HandValue(playerHand) > 21)
        {
            gameState = GameState.RoundOver;
        }
        onGameStateChanged?.Invoke();
    }

    public void PlayerStand()
    {
        if (gameState != GameState.PlayerTurn) return;
        gameState = GameState.DealerTurn;
        DealerPlay();
        onGameStateChanged?.Invoke();
    }

    private void DealerPlay()
    {
        while (HandValue(dealerHand) < 17)
        {
            dealerHand.Add(DrawCard());
        }
        gameState = GameState.RoundOver;
    }

    public int HandValue(List<int> hand)
    {
        int value = 0;
        int aces = 0;
        foreach (int card in hand)
        {
            int v = card > 10 ? 10 : card;
            if (v == 1) aces++;
            value += v;
        }
        // Handle ace as 11 if possible
        while (aces > 0 && value <= 11)
        {
            value += 10;
            aces--;
        }
        return value;
    }

    public string GetResult()
    {
        int playerVal = HandValue(playerHand);
        int dealerVal = HandValue(dealerHand);
        if (playerVal > 21) return "Bust! Dealer Wins.";
        if (dealerVal > 21) return "Dealer Busts! Player Wins.";
        if (playerVal > dealerVal) return "Player Wins!";
        if (playerVal < dealerVal) return "Dealer Wins!";
        return "Push (Tie).";
    }
}
