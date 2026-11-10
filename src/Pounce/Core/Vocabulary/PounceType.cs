namespace Pounce.Core;

record PounceType(string Name);

record Signature(List<PounceType> OperandTypes, PounceType OutputType)
{
    public virtual bool Equals(Signature? other)
    {
        return other is not null
        && OutputType == other.OutputType
        && OperandTypes.SequenceEqual(other.OperandTypes);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(OutputType);
        foreach (PounceType type in OperandTypes) hash.Add(type);
        return hash.ToHashCode();
    }
}