using System.Net.Http.Headers;
using System.Net.Http.Json;
using Tripletex.Api.Pagination;

namespace Tripletex.Api.Operations;

public sealed class ExpenseAttachmentOperations(HttpClient http)
{
    public async Task<Stream> DownloadAsync(int travelExpenseId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"travelExpense/{travelExpenseId}/attachment", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync(ct);
    }

    public async Task<TravelExpense> UploadAsync(
        int travelExpenseId,
        Stream fileStream,
        string fileName,
        bool createNewCost = false,
        CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(fileStream);
        streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        content.Add(streamContent, "file", fileName);

        var url = $"travelExpense/{travelExpenseId}/attachment";
        if (createNewCost) url += "?createNewCost=true";

        var response = await http.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SingleResponse<TravelExpense>>(ct);
        return result?.Value ?? throw new InvalidOperationException("Failed to upload attachment");
    }

    public async Task<TravelExpense> UploadAsync(
        int travelExpenseId,
        string filePath,
        bool createNewCost = false,
        CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(filePath);
        return await UploadAsync(travelExpenseId, stream, Path.GetFileName(filePath), createNewCost, ct);
    }

    public async Task<TravelExpense> UploadMultipleAsync(
        int travelExpenseId,
        IEnumerable<(Stream Stream, string FileName)> files,
        bool createNewCost = false,
        CancellationToken ct = default)
    {
        using var content = new MultipartFormDataContent();
        foreach (var (stream, fileName) in files)
        {
            var streamContent = new StreamContent(stream);
            streamContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            content.Add(streamContent, "file", fileName);
        }

        var url = $"travelExpense/{travelExpenseId}/attachment/list";
        if (createNewCost) url += "?createNewCost=true";

        var response = await http.PostAsync(url, content, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SingleResponse<TravelExpense>>(ct);
        return result?.Value ?? throw new InvalidOperationException("Failed to upload attachments");
    }

    public async Task DeleteAsync(int travelExpenseId, int version, bool sendToInbox = false, CancellationToken ct = default)
    {
        var parts = new List<string> { $"version={version}" };
        if (sendToInbox) parts.Add("sendToInbox=true");

        var url = $"travelExpense/{travelExpenseId}/attachment?" + string.Join("&", parts);
        var response = await http.DeleteAsync(url, ct);
        response.EnsureSuccessStatusCode();
    }
}
