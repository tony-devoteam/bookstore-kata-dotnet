using Bookstore.Api.Domain;

namespace Bookstore.Api.Application;

public sealed record CreateBookCommand(
    string? Title,
    string? Author,
    string? Category,
    int? Quantity,
    string? Description,
    string? Language,
    bool? Active,
    DateTimeOffset? InactiveDate,
    string? Publisher,
    DateTimeOffset? PublisherDate);

public sealed record UpdateBookCommand(int? Quantity, bool? Active);

public sealed record BookQuery(int? CategoryId, string? Title, string? Author, int Skip, int Limit);

public interface IBookRepository
{
    Task<IReadOnlyList<Book>> ListAsync(BookQuery query, CancellationToken cancellationToken);
    Task<Book?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<Book> InsertAsync(Book book, CancellationToken cancellationToken);
    Task<Book?> UpdateStockAsync(string id, BookStockChange change, CancellationToken cancellationToken);
    Task<bool> PingAsync(CancellationToken cancellationToken);
}

public interface IBookIdGenerator
{
    string NewId();
}

public interface IBookService
{
    Task<IReadOnlyList<Book>> ListAsync(
        string? category,
        string? title,
        string? author,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<Book?> GetByIdAsync(string id, CancellationToken cancellationToken);
    Task<Book> RegisterAsync(CreateBookCommand command, CancellationToken cancellationToken);
    Task<Book?> UpdateAsync(string id, UpdateBookCommand command, CancellationToken cancellationToken);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken);
}
