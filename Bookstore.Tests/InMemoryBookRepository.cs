using Bookstore.Api.Application;
using Bookstore.Api.Domain;

namespace Bookstore.Tests;

internal sealed class InMemoryBookRepository : IBookRepository
{
    private readonly List<Book> _books = [];
    private readonly bool _isHealthy;

    public InMemoryBookRepository(bool isHealthy = true)
    {
        _isHealthy = isHealthy;
    }

    public Task<IReadOnlyList<Book>> ListAsync(BookQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IEnumerable<Book> books = _books;
        if (query.CategoryId is int categoryId)
            books = books.Where(book => book.CategoryId == categoryId);
        if (query.Title is not null)
            books = books.Where(book => book.Title == query.Title);
        if (query.Author is not null)
            books = books.Where(book => book.Author == query.Author);

        IReadOnlyList<Book> page = books.Skip(query.Skip).Take(query.Limit).ToArray();
        return Task.FromResult(page);
    }

    public Task<Book?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_books.FirstOrDefault(book => book.Id == id));
    }

    public Task<Book> InsertAsync(Book book, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _books.Add(book);
        return Task.FromResult(book);
    }

    public Task<Book?> UpdateStockAsync(string id, BookStockChange change, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var index = _books.FindIndex(existing => existing.Id == id);
        if (index < 0)
            return Task.FromResult<Book?>(null);

        var updated = _books[index].ApplyStockChange(change);
        _books[index] = updated;
        return Task.FromResult<Book?>(updated);
    }

    public Task<bool> PingAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_isHealthy);
    }
}
