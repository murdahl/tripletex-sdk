using System.Net.Http.Headers;
using System.Text;

namespace Tripletex.Api.Authentication;

internal sealed class BasicAuthHandler(SessionTokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var (companyId, sessionToken) = await tokenProvider.GetSessionAsync(cancellationToken);

        var credentials = Convert.ToBase64String(
            Encoding.ASCII.GetBytes($"{companyId}:{sessionToken}"));

        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        return await base.SendAsync(request, cancellationToken);
    }
}
