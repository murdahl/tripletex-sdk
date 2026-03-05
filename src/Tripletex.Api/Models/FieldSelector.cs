namespace Tripletex.Api.Models;

public sealed class FieldSelector
{
    private readonly List<string> _fields = [];

    public FieldSelector Add(string field)
    {
        _fields.Add(field);
        return this;
    }

    public FieldSelector Add(string parent, params string[] children)
    {
        foreach (var child in children)
            _fields.Add($"{parent}.{child}");
        return this;
    }

    public override string ToString() => string.Join(",", _fields);

    public static implicit operator string(FieldSelector selector) => selector.ToString();
}
