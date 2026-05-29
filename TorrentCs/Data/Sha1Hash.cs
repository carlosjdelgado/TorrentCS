namespace TorrentCs.Data;

public sealed class Sha1Hash : IEquatable<Sha1Hash>
{
    public const int Length = 20;
    public static readonly Sha1Hash Empty = new(new byte[Length]);

    public Sha1Hash(byte[] value)
    {
        if (value.Length != Length)
            throw new ArgumentException($"Value must be {Length} bytes.", nameof(value));
        Value = value;
    }

    public byte[] Value { get; }

    public static implicit operator byte[](Sha1Hash hash) => hash.Value;

    public static bool operator ==(Sha1Hash? left, Sha1Hash? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(Sha1Hash? left, Sha1Hash? right) => !(left == right);

    public bool Equals(Sha1Hash? other)
    {
        if (other is null) return false;
        return Value.SequenceEqual(other.Value);
    }

    public override bool Equals(object? obj) => obj is Sha1Hash other && Equals(other);

    public override int GetHashCode() => BitConverter.ToInt32(Value, 0);

    public override string ToString() => Convert.ToBase64String(Value)[..8];
}
