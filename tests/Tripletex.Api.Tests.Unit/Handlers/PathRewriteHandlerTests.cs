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
            ["/approveList"] = "/:approveList",
            ["/approveSubscriptionInvoice"] = "/:approveSubscriptionInvoice",
            ["/create"] = "/:create",
            ["/createCreditNote"] = "/:createCreditNote",
            ["/createReminder"] = "/:createReminder",
            ["/last"] = "/>last",
            ["/lastClosed"] = "/>lastClosed",
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
    [InlineData("/v2/invoice/123/createCreditNote", "/v2/invoice/123/:createCreditNote")]
    [InlineData("/v2/invoice/123/createReminder", "/v2/invoice/123/:createReminder")]
    [InlineData("/v2/timesheet/month/approveList", "/v2/timesheet/month/:approveList")]
    [InlineData("/v2/invoice/123/approveSubscriptionInvoice", "/v2/invoice/123/:approveSubscriptionInvoice")]
    [InlineData("/v2/timesheet/entry/lastClosed", "/v2/timesheet/entry/>lastClosed")]
    public void RewritePath_TransformsCorrectly(string input, string expected)
    {
        var result = _handler.RewritePath(input);
        result.Should().Be(expected);
    }
}
