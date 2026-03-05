using System.Net;
using System.Text.Json;
using FluentAssertions;
using Tripletex.Api.Pagination;

namespace Tripletex.Api.Tests.Unit.Pagination;

public class PaginationExtensionsTests
{
    [Fact]
    public async Task PaginateAsync_SinglePage_ReturnsAllItems()
    {
        var page = new ListResponse<TestItem>
        {
            FullResultSize = 3,
            From = 0,
            Count = 3,
            Values = [new() { Id = 1 }, new() { Id = 2 }, new() { Id = 3 }]
        };

        var items = new List<TestItem>();
        await foreach (var item in PaginationExtensions.PaginateAsync<TestItem>(
            (from, count, ct) => CreateResponse(page)))
        {
            items.Add(item);
        }

        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task PaginateAsync_MultiplePages_PaginatesCorrectly()
    {
        var pages = new Queue<ListResponse<TestItem>>([
            new() { FullResultSize = 5, From = 0, Count = 2, Values = [new() { Id = 1 }, new() { Id = 2 }] },
            new() { FullResultSize = 5, From = 2, Count = 2, Values = [new() { Id = 3 }, new() { Id = 4 }] },
            new() { FullResultSize = 5, From = 4, Count = 1, Values = [new() { Id = 5 }] },
        ]);

        var items = new List<TestItem>();
        await foreach (var item in PaginationExtensions.PaginateAsync<TestItem>(
            (from, count, ct) => CreateResponse(pages.Dequeue()),
            pageSize: 2))
        {
            items.Add(item);
        }

        items.Should().HaveCount(5);
        items.Select(i => i.Id).Should().BeEquivalentTo([1, 2, 3, 4, 5]);
    }

    [Fact]
    public async Task PaginateAsync_EmptyResponse_ReturnsEmpty()
    {
        var page = new ListResponse<TestItem>
        {
            FullResultSize = 0,
            From = 0,
            Count = 0,
            Values = []
        };

        var items = new List<TestItem>();
        await foreach (var item in PaginationExtensions.PaginateAsync<TestItem>(
            (from, count, ct) => CreateResponse(page)))
        {
            items.Add(item);
        }

        items.Should().BeEmpty();
    }

    private static Task<HttpResponseMessage> CreateResponse<T>(ListResponse<T> page)
    {
        var json = JsonSerializer.Serialize(page);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        });
    }

    private sealed class TestItem
    {
        public int Id { get; set; }
    }
}
