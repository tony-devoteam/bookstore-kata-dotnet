using Bookstore.Api.Domain;

namespace Bookstore.Api.Application;

public sealed class BookService : IBookService
{
    public const int DEFAULT_PAGE_SIZE = 50;
    public const int MAX_PAGE_SIZE = 100;

    private readonly IBookRepository _repository;
    private readonly IBookIdGenerator _idGenerator;
    private readonly TimeProvider _timeProvider;

    public BookService(
        IBookRepository repository,
        IBookIdGenerator idGenerator,
        TimeProvider timeProvider)
    {
        _repository = repository;
        _idGenerator = idGenerator;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<Book>> ListAsync(
        string? category,
        string? title,
        string? author,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        int? categoryId = null;
        if (category is not null)
        {
            if (!BookCategory.TryGetId(category, out var foundCategoryId))
                throw new BookValidationException("Category must be one of the supported category names.");

            categoryId = foundCategoryId;
        }

        if (page < 1)
            throw new BookValidationException("Page must be greater than zero.");

        if (pageSize is < 1 or > MAX_PAGE_SIZE)
            throw new BookValidationException($"Page size must be between 1 and {MAX_PAGE_SIZE}.");

        var skip = (long)(page - 1) * pageSize;
        if (skip > int.MaxValue)
            throw new BookValidationException("The requested page is out of range.");

        var query = new BookQuery(categoryId, title, author, (int)skip, pageSize);
        return await _repository.ListAsync(query, cancellationToken);
    }

    public Task<Book?> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        ValidateId(id);
        return _repository.GetByIdAsync(id.ToLowerInvariant(), cancellationToken);
    }

    public async Task<Book> RegisterAsync(
        CreateBookCommand command,
        CancellationToken cancellationToken)
    {
        var categoryId = 0;
        if (command.Category is not null && !BookCategory.TryGetId(command.Category, out categoryId))
            throw new BookValidationException("Category must be one of the supported category names.");

        var book = Book.Register(
            _idGenerator.NewId(),
            command.Title,
            command.Author,
            categoryId,
            command.Quantity ?? 0,
            command.Description,
            command.Language,
            command.Active ?? true,
            command.InactiveDate,
            command.Publisher,
            command.PublisherDate,
            _timeProvider.GetUtcNow());

        return await _repository.InsertAsync(book, cancellationToken);
    }

    public async Task<Book?> UpdateAsync(
        string id,
        UpdateBookCommand command,
        CancellationToken cancellationToken)
    {
        ValidateId(id);

        var change = BookStockChange.Create(
            command.Quantity,
            command.Active,
            _timeProvider.GetUtcNow());

        return await _repository.UpdateStockAsync(id.ToLowerInvariant(), change, cancellationToken);
    }

    public Task<bool> IsHealthyAsync(CancellationToken cancellationToken) =>
        _repository.PingAsync(cancellationToken);

    private static void ValidateId(string? id)
    {
        if (!Book.IsValidId(id))
            throw new BookValidationException("Book id must be a 24-character hexadecimal ObjectId.");
    }
}
