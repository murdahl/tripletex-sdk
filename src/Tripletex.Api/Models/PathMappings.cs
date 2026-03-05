namespace Tripletex.Api.Models;

internal static class PathMappings
{
    // This will be replaced by the spec preprocessor with generated mappings.
    // For now, provide common known mappings as defaults.
    public static readonly IReadOnlyDictionary<string, string> Default = new Dictionary<string, string>
    {
        // Action paths: sanitized → original
        ["/approve"] = "/:approve",
        ["/reject"] = "/:reject",
        ["/create"] = "/:create",
        ["/send"] = "/:send",
        ["/createCreditNote"] = "/:createCreditNote",
        ["/import"] = "/:import",
        ["/complete"] = "/:complete",
        ["/reopen"] = "/:reopen",

        // Summary/aggregation paths
        ["/last"] = "/>last",
        ["/summary"] = "/>summary",

        // Special mid-segment colons
        ["/unmatchedCsv"] = "/unmatched:csv",
    };
}
