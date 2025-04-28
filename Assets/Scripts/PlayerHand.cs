using Unity.Netcode;
using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct PlayerHand : INetworkSerializable, IEquatable<PlayerHand>
{
    public const int MaxCards = 32;
    public int count;
    public fixed int cards[MaxCards];

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref count);
        for (int i = 0; i < count; i++)
        {
            int value = serializer.IsReader ? 0 : cards[i];
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
    }

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

    public void Add(int card)
    {
        if (count < MaxCards)
        {
            cards[count] = card;
            count++;
        }
    }
}

