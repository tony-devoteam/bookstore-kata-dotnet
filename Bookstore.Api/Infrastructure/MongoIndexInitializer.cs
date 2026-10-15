using MongoDB.Driver;

namespace Bookstore.Api.Infrastructure;

public sealed class MongoIndexInitializer(
    IMongoClient mongoClient,
    IConfiguration configuration,
    ILogger<MongoIndexInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var databaseName = configuration["MONGODB_DATABASE"] ?? "bookstore";
        var collectionName = configuration["MONGODB_COLLECTION"] ?? "books";
        var collection = mongoClient.GetDatabase(databaseName).GetCollection<BookDocument>(collectionName);
        var keys = Builders<BookDocument>.IndexKeys;
        var indexes = new[]
        {
            new CreateIndexModel<BookDocument>(
                keys.Ascending(book => book.CategoryId).Ascending(book => book.Id),
                new CreateIndexOptions { Name = "category_id_ascending" }),
            new CreateIndexModel<BookDocument>(
                keys.Ascending(book => book.Title).Ascending(book => book.Id),
                new CreateIndexOptions { Name = "title_id_ascending" }),
            new CreateIndexModel<BookDocument>(
                keys.Ascending(book => book.Author).Ascending(book => book.Id),
                new CreateIndexOptions { Name = "author_id_ascending" })
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken);
        logger.LogInformation("Book catalogue indexes are ready");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
