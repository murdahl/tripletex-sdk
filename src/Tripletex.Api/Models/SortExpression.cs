namespace Tripletex.Api.Models;

public sealed class SortExpression
{
    private readonly List<string> _clauses = [];

    public SortExpression Ascending(string field)
    {
        _clauses.Add(field);
        return this;
    }

    public SortExpression Descending(string field)
    {
        _clauses.Add($"-{field}");
        return this;
    }

    public override string ToString() => string.Join(",", _clauses);

    public static implicit operator string(SortExpression expression) => expression.ToString();
}
