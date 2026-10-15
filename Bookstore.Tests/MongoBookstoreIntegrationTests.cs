using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Testcontainers.MongoDb;
using Xunit;

namespace Bookstore.Tests;

public sealed class MongoBookstoreIntegrationTests
{
    [Fact]
    public async Task BookLifecycle_WhenStoredInMongoDB_CanBeSearchedAndUpdated()
    {
        await using var mongo = new MongoDbBuilder("mongo:7.0").Build();
        await mongo.StartAsync();
        using var factory = new MongoBookstoreApiFactory(mongo.GetConnectionString());
        using var client = factory.CreateClient();

        using var createResponse = await client.PostAsJsonAsync(
            "/api/v1/books",
            new
            {
                title = "Test Driven Development",
                author = "Kent Beck",
                category = "Programming",
                quantity = 3,
                publisherDate = "2002-11-08T10:00:00+02:00"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString();
        Assert.Equal(6, created.GetProperty("categoryId").GetInt32());

        using var getResponse = await client.GetAsync($"/api/v1/books/{id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var loaded = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Test Driven Development", loaded.GetProperty("title").GetString());
        Assert.Equal(
            new DateTimeOffset(2002, 11, 8, 8, 0, 0, TimeSpan.Zero),
            DateTimeOffset.Parse(loaded.GetProperty("publisherDate").GetString()!).ToUniversalTime());

        using var updateResponse = await client.PatchAsJsonAsync($"/api/v1/books/{id}", new { quantity = 10 });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(10, updated.GetProperty("quantity").GetInt32());
        Assert.True(updated.GetProperty("active").GetBoolean());

        using var categoryResponse = await client.GetAsync("/api/v1/books?category=programming&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, categoryResponse.StatusCode);
        var categoryBooks = await categoryResponse.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.Single(categoryBooks!);

        using var searchResponse = await client.GetAsync("/api/v1/books?author=Kent%20Beck");
        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        var results = await searchResponse.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.Single(results!);
        Assert.Equal(id, results![0].GetProperty("id").GetString());

        using var healthResponse = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
    }
}
