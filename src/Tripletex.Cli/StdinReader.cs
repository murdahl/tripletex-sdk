using System.Text.Json;

namespace Tripletex.Cli;

public static class StdinReader
{
    public static List<int>? TryReadIds()
    {
        if (!Console.IsInputRedirected)
            return null;

        var input = Console.In.ReadToEnd().Trim();
        if (string.IsNullOrEmpty(input))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(input);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                var ids = new List<int>();
                foreach (var element in root.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.Number)
                    {
                        ids.Add(element.GetInt32());
                    }
                    else if (element.ValueKind == JsonValueKind.Object)
                    {
                        ids.Add(ExtractId(element));
                    }
                }
                return ids.Count > 0 ? ids : null;
            }

            if (root.ValueKind == JsonValueKind.Number)
                return [root.GetInt32()];

            if (root.ValueKind == JsonValueKind.Object)
                return [ExtractId(root)];
        }
        catch (JsonException)
        {
            // Not JSON — try plain text, one integer per line
        }

        var result = new List<int>();
        foreach (var line in input.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (int.TryParse(line, out var id))
                result.Add(id);
        }

        return result.Count > 0 ? result : null;
    }

    private static int ExtractId(JsonElement obj)
    {
        if (obj.TryGetProperty("Id", out var idProp) && idProp.ValueKind == JsonValueKind.Number)
            return idProp.GetInt32();
        if (obj.TryGetProperty("id", out var idLower) && idLower.ValueKind == JsonValueKind.Number)
            return idLower.GetInt32();
        throw new InvalidOperationException("JSON object has no 'Id' or 'id' property.");
    }
}
