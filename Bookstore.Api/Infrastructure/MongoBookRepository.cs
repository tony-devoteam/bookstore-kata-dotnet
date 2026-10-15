using Bookstore.Api.Application;
using Bookstore.Api.Domain;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Bookstore.Api.Infrastructure;

public sealed class MongoBookRepository : IBookRepository
{
    private readonly IMongoCollection<BookDocument> _books;

    public MongoBookRepository(IMongoClient mongoClient, IConfiguration configuration)
    {
        var databaseName = configuration["MONGODB_DATABASE"] ?? "bookstore";
        var collectionName = configuration["MONGODB_COLLECTION"] ?? "books";
        _books = mongoClient.GetDatabase(databaseName).GetCollection<BookDocument>(collectionName);
    }

    public async Task<IReadOnlyList<Book>> ListAsync(BookQuery query, CancellationToken cancellationToken)
    {
        var filters = new List<FilterDefinition<BookDocument>>();
        var builder = Builders<BookDocument>.Filter;

        if (query.CategoryId is int categoryId)
            filters.Add(builder.Eq(book => book.CategoryId, categoryId));
        if (query.Title is not null)
            filters.Add(builder.Eq(book => book.Title, query.Title));
        if (query.Author is not null)
            filters.Add(builder.Eq(book => book.Author, query.Author));

        var filter = filters.Count == 0 ? builder.Empty : builder.And(filters);
        var documents = await _books.Find(filter)
            .SortBy(book => book.Id)
            .Skip(query.Skip)
            .Limit(query.Limit)
            .ToListAsync(cancellationToken);

        return documents.Select(ToDomain).ToArray();
    }

    public async Task<Book?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        var document = await _books.Find(book => book.Id == id).FirstOrDefaultAsync(cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task<Book> InsertAsync(Book book, CancellationToken cancellationToken)
    {
        await _books.InsertOneAsync(ToDocument(book), cancellationToken: cancellationToken);
        return book;
    }

    public async Task<Book?> UpdateStockAsync(
        string id,
        BookStockChange change,
        CancellationToken cancellationToken)
    {
        var update = Builders<BookDocument>.Update;
        var updates = new List<UpdateDefinition<BookDocument>>();
        if (change.Quantity is int quantity)
            updates.Add(update.Set(book => book.Quantity, quantity));
        if (change.Active is bool active)
        {
            updates.Add(update.Set(book => book.Active, active));
            updates.Add(update.Set(book => book.InactiveDate, change.InactiveDate?.UtcDateTime));
        }

        var document = await _books.FindOneAndUpdateAsync(
            book => book.Id == id,
            update.Combine(updates),
            new FindOneAndUpdateOptions<BookDocument> { ReturnDocument = ReturnDocument.After },
            cancellationToken);
        return document is null ? null : ToDomain(document);
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _books.Database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1),
                cancellationToken: cancellationToken);
            return true;
        }
        catch (MongoException)
        {
            return false;
        }
    }

    internal static Book ToDomain(BookDocument document) => Book.Restore(
        document.Id,
        document.Title,
        document.Author,
        document.CategoryId,
        document.Quantity,
        document.Description,
        document.Language,
        document.Active,
        ToDateTimeOffset(document.InactiveDate),
        document.Publisher,
        ToDateTimeOffset(document.PublisherDate));

    internal static BookDocument ToDocument(Book book) => new()
    {
        Id = book.Id,
        Title = book.Title,
        Author = book.Author,
        CategoryId = book.CategoryId,
        Quantity = book.Quantity,
        Description = book.Description,
        Language = book.Language,
        Active = book.Active,
        InactiveDate = book.InactiveDate?.UtcDateTime,
        Publisher = book.Publisher,
        PublisherDate = book.PublisherDate?.UtcDateTime
    };

    private static DateTimeOffset? ToDateTimeOffset(DateTime? value) =>
        value is null
            ? null
            : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
}

[BsonIgnoreExtraElements]
internal sealed class BookDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("title")]
    public string? Title { get; set; }

    [BsonElement("author")]
    public string? Author { get; set; }

    [BsonElement("categoryId")]
    public int CategoryId { get; set; }

    [BsonElement("quantity")]
    public int Quantity { get; set; }

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("language")]
    public string? Language { get; set; }

    [BsonElement("active")]
    public bool Active { get; set; }

    [BsonElement("inactiveDate")]
    public DateTime? InactiveDate { get; set; }

    [BsonElement("publisher")]
    public string? Publisher { get; set; }

    [BsonElement("publisherDate")]
    public DateTime? PublisherDate { get; set; }
}
