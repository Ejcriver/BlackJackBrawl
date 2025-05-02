using Unity.Netcode;
using UnityEngine;

public class SpawnedCardHelper : MonoBehaviour
{
    // Helper to convert card int to sprite path for UI display
    public static string CardIntToSpritePath(int cardInt)
    {
        // Suits: 0 = Hearts, 1 = Diamonds, 2 = Clubs, 3 = Spades
        // Card values: 1 = Ace, 2-10 = Number, 11 = Jack, 12 = Queen, 13 = King
        int suit = cardInt / 100; // e.g., 1xx = Hearts, 2xx = Diamonds, etc.
        int value = cardInt % 100; // 1-13
        string suitName = "heart";
        string suitFolder = "Hearts";
        switch (suit)
        {
            case 1: suitName = "heart"; suitFolder = "Hearts"; break;
            case 2: suitName = "diamond"; suitFolder = "Diamonds"; break;
            case 3: suitName = "club"; suitFolder = "Clubs"; break;
            case 4: suitName = "spade"; suitFolder = "Spades"; break;
        }
        // Path: 2D Cards Game Art Pack/Sprites/Standard 52 Cards/Standard Cards/{SuitFolder}/{value}_{suit_name}
        return $"2D Cards Game Art Pack/Sprites/Standard 52 Cards/Standard Cards/{suitFolder}/{value}_{suitName}";
    }

}
