using UnityEngine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BlackjackUIController : MonoBehaviour
{
    public UIDocument blackjackUIDocument;
    public BlackjackGameLogic blackjackGameLogic;

    private Button hitButton;
    private Button standButton;
    private VisualElement playerHandPanel;
    private VisualElement dealerHandPanel;
    private Label messageLabel;
    private Label playerLabel;
    private Label dealerLabel;

    void Awake()
    {
        if (blackjackUIDocument == null)
            blackjackUIDocument = GetComponent<UIDocument>();
        if (blackjackGameLogic == null)
            blackjackGameLogic = FindFirstObjectByType<BlackjackGameLogic>();
    }

    void OnEnable()
    {
        if (blackjackUIDocument != null)
        {
            var root = blackjackUIDocument.rootVisualElement;
            hitButton = root.Q<Button>("HitButton");
            standButton = root.Q<Button>("StandButton");
            playerHandPanel = root.Q<VisualElement>("PlayerHand");
            dealerHandPanel = root.Q<VisualElement>("DealerHand");
            messageLabel = root.Q<Label>("MessageLabel");
            playerLabel = root.Q<Label>("PlayerLabel");
            dealerLabel = root.Q<Label>("DealerLabel");

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
        // Update dealer hand
        dealerHandPanel.Clear();
        foreach (int card in blackjackGameLogic.dealerHand)
        {
            var cardLabel = new Label(CardToString(card));
            cardLabel.AddToClassList("info-label");
            dealerHandPanel.Add(cardLabel);
        }
        // Update hand values
        if (playerLabel != null)
            playerLabel.text = $"Your Hand ({blackjackGameLogic.HandValue(blackjackGameLogic.playerHand)})";
        if (dealerLabel != null)
            dealerLabel.text = $"Dealer ({blackjackGameLogic.HandValue(blackjackGameLogic.dealerHand)})";
        // Show result if round is over
        if (blackjackGameLogic.gameState == BlackjackGameLogic.GameState.RoundOver)
        {
            if (messageLabel != null)
                messageLabel.text = blackjackGameLogic.GetResult();
            if (hitButton != null) hitButton.SetEnabled(false);
            if (standButton != null) standButton.SetEnabled(false);
        }
        else
        {
            if (messageLabel != null)
                messageLabel.text = "";
            if (hitButton != null) hitButton.SetEnabled(true);
            if (standButton != null) standButton.SetEnabled(true);
        }
    }

    private string CardToString(int card)
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
}

