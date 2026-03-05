using FluentAssertions;
using Tripletex.Api.Handlers;

namespace Tripletex.Api.Tests.Unit.Handlers;

public class PathRewriteHandlerTests
{
    private readonly PathRewriteHandler _handler;

    public PathRewriteHandlerTests()
    {
        var mappings = new Dictionary<string, string>
        {
            ["/approve"] = "/:approve",
            ["/create"] = "/:create",
            ["/last"] = "/>last",
            ["/unmatchedCsv"] = "/unmatched:csv",
        };

        _handler = new PathRewriteHandler(mappings) { InnerHandler = new MockHandler(new(System.Net.HttpStatusCode.OK)) };
    }

    [Theory]
    [InlineData("/v2/timesheet/month/approve", "/v2/timesheet/month/:approve")]
    [InlineData("/v2/token/session/create", "/v2/token/session/:create")]
    [InlineData("/v2/timesheet/entry/last", "/v2/timesheet/entry/>last")]
    [InlineData("/v2/bank/unmatchedCsv", "/v2/bank/unmatched:csv")]
    [InlineData("/v2/employee/123", "/v2/employee/123")]  // no rewrite needed
    public void RewritePath_TransformsCorrectly(string input, string expected)
    {
        var result = _handler.RewritePath(input);
        result.Should().Be(expected);
    }
}
