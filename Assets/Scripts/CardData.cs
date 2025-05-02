using Unity.Netcode;
using System;

public enum CardType : byte
{
    Standard = 0,
    Power = 1,
    // Add more types as needed
}

[Serializable]
public struct CardData : INetworkSerializable, IEquatable<CardData>
{
    public CardType cardType;
    public int value;    // 1-13 for standard cards, or custom for power cards
    public int suit;     // 0-3 for standard cards (optional for power cards)
    public int powerId;  // For power cards: a unique identifier or ability type

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref cardType);
        serializer.SerializeValue(ref value);
        serializer.SerializeValue(ref suit);
        serializer.SerializeValue(ref powerId);
    }

    public bool Equals(CardData other)
    {
        return cardType == other.cardType && value == other.value && suit == other.suit && powerId == other.powerId;
    }
}
