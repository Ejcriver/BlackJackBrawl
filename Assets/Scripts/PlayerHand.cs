using Unity.Netcode;
using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct PlayerHand : INetworkSerializable, IEquatable<PlayerHand>
{
    public const int MaxCards = 32;
    public int count;
    public fixed long cards[MaxCards]; // Store CardData as long (bit-packed)

    // Helper to convert CardData <-> long for storage
    private static long ToLong(CardData card)
    {
        // Pack CardType (1 byte), value (4 bytes), suit (4 bytes), powerId (4 bytes) into a long
        long result = (long)card.cardType;
        result |= ((long)card.value << 8);
        result |= ((long)card.suit << 20);
        result |= ((long)card.powerId << 28);
        return result;
    }
    private static CardData FromLong(long data)
    {
        CardData card = new CardData();
        card.cardType = (CardType)(data & 0xFF);
        card.value = (int)((data >> 8) & 0xFFF);
        card.suit = (int)((data >> 20) & 0xFF);
        card.powerId = (int)((data >> 28) & 0xFFF);
        return card;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref count);
        for (int i = 0; i < count; i++)
        {
            long value = serializer.IsReader ? 0 : cards[i];
            serializer.SerializeValue(ref value);
            if (serializer.IsReader)
                cards[i] = value;
        }
    }

    public bool Equals(PlayerHand other)
    {
        if (count != other.count)
            return false;
        for (int i = 0; i < count; i++)
            if (cards[i] != other.cards[i])
                return false;
        return true;
    } // No change needed, as comparison is still by long

    public override bool Equals(object obj)
    {
        return obj is PlayerHand other && Equals(other);
    }

    public override int GetHashCode()
    {
        int hash = 17;
        for (int i = 0; i < count; i++)
            hash = hash * 31 + cards[i].GetHashCode();
        return hash;
    }

    public void Clear()
    {
        count = 0;
    }

    public void Add(CardData card)
    {
        if (count < MaxCards)
        {
            cards[count] = ToLong(card);
            count++;
        }
    }
    public CardData Get(int idx)
    {
        if (idx < 0 || idx >= count) return default;
        return FromLong(cards[idx]);
    }
}

