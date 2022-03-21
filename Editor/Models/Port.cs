using System;

[System.Serializable]
public class Port : IComparable<Port>, IEquatable<Port>
{
    public Protocol Protocol { get; set; }
    public int Number { get; set; }

    public int CompareTo(Port other)
    {
        if (Protocol != other.Protocol)
        {
            return (int)Protocol - (int)other.Protocol;
        }

        return Number - other.Number;
    }

    public bool Equals(Port other)
    {
        return Protocol == other.Protocol && Number == other.Number;
    }

    public override int GetHashCode()
    {
        return (int)Protocol * 1878217 + Number;
    }
}
