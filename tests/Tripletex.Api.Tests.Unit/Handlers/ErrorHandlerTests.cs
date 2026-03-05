using System.Net;
using System.Text.Json;
using FluentAssertions;
using Tripletex.Api.Handlers;
using Tripletex.Api.Models;

namespace Tripletex.Api.Tests.Unit.Handlers;

public class ErrorHandlerTests
{
    [Fact]
    public async Task SuccessResponse_PassesThrough()
    {
        var handler = CreateHandler(HttpStatusCode.OK, """{"value": {}}""");
        var client = new HttpClient(handler);

        var response = await client.GetAsync("https://example.com/test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ErrorResponse_ThrowsTripletexApiException()
    {
        var errorBody = JsonSerializer.Serialize(new TripletexErrorResponse
        {
            Status = 404,
            Code = "NOT_FOUND",
            Message = "Resource not found",
            DeveloperMessage = "Employee with id 999 not found",
            RequestId = "abc-123"
        });

        var handler = CreateHandler(HttpStatusCode.NotFound, errorBody);
        var client = new HttpClient(handler);

        var act = () => client.GetAsync("https://example.com/test");

        var ex = await act.Should().ThrowAsync<TripletexApiException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.ErrorCode.Should().Be("NOT_FOUND");
        ex.Which.Message.Should().Be("Resource not found");
        ex.Which.DeveloperMessage.Should().Be("Employee with id 999 not found");
        ex.Which.RequestId.Should().Be("abc-123");
    }

    [Fact]
    public async Task ErrorWithValidationMessages_IncludesValidation()
    {
        var errorBody = JsonSerializer.Serialize(new TripletexErrorResponse
        {
            Status = 422,
            Message = "Validation failed",
            ValidationMessages = [new() { Field = "hours", Message = "Must be positive" }]
        });

        var handler = CreateHandler(HttpStatusCode.UnprocessableEntity, errorBody);
        var client = new HttpClient(handler);

        var act = () => client.GetAsync("https://example.com/test");

        var ex = await act.Should().ThrowAsync<TripletexApiException>();
        ex.Which.ValidationMessages.Should().HaveCount(1);
        ex.Which.ValidationMessages[0].Field.Should().Be("hours");
    }

    [Fact]
    public async Task NonJsonErrorBody_ThrowsWithRawBody()
    {
        var handler = CreateHandler(HttpStatusCode.InternalServerError, "Internal Server Error");
        var client = new HttpClient(handler);

        var act = () => client.GetAsync("https://example.com/test");

        var ex = await act.Should().ThrowAsync<TripletexApiException>();
        ex.Which.StatusCode.Should().Be(500);
        ex.Which.Message.Should().Be("Internal Server Error");
    }

    private static ErrorHandler CreateHandler(HttpStatusCode statusCode, string body)
    {
        var inner = new MockHandler(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        });

        return new ErrorHandler { InnerHandler = inner };
    }
}

internal class MockHandler(HttpResponseMessage response) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(response);
}
