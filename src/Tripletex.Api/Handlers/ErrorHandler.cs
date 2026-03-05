using System.Net;
using System.Text.Json;
using Tripletex.Api.Models;

namespace Tripletex.Api.Handlers;

internal sealed class ErrorHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
            return response;

        using (response)
        {
            var statusCode = (int)response.StatusCode;
            string? body = null;

            try
            {
                body = await response.Content.ReadAsStringAsync(cancellationToken);
                var error = JsonSerializer.Deserialize<TripletexErrorResponse>(body);

                if (error is not null)
                {
                    throw new TripletexApiException(
                        statusCode,
                        error.Message,
                        error.Code,
                        error.DeveloperMessage,
                        error.RequestId,
                        error.ValidationMessages?.AsReadOnly());
                }
            }
            catch (TripletexApiException)
            {
                throw;
            }
            catch
            {
                // JSON parse failed — throw generic
            }

            throw new TripletexApiException(statusCode, body ?? $"HTTP {statusCode}");
        }
    }
}
