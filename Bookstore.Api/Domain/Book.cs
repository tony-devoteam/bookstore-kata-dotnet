namespace Bookstore.Api.Domain;

public sealed record Book
{
    private Book(
        string id,
        string title,
        string? author,
        int categoryId,
        int quantity,
        string? description,
        string? language,
        bool active,
        DateTimeOffset? inactiveDate,
        string? publisher,
        DateTimeOffset? publisherDate)
    {
        Id = id;
        Title = title;
        Author = author;
        CategoryId = categoryId;
        Quantity = quantity;
        Description = description;
        Language = language;
        Active = active;
        InactiveDate = inactiveDate;
        Publisher = publisher;
        PublisherDate = publisherDate;
    }

    public string Id { get; private init; }
    public string Title { get; private init; }
    public string? Author { get; private init; }
    public int CategoryId { get; private init; }
    public int Quantity { get; private init; }
    public string? Description { get; private init; }
    public string? Language { get; private init; }
    public bool Active { get; private init; }
    public DateTimeOffset? InactiveDate { get; private init; }
    public string? Publisher { get; private init; }
    public DateTimeOffset? PublisherDate { get; private init; }

    public static Book Register(
        string id,
        string? title,
        string? author,
        int categoryId,
        int quantity,
        string? description,
        string? language,
        bool active,
        DateTimeOffset? inactiveDate,
        string? publisher,
        DateTimeOffset? publisherDate,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new BookValidationException("Title is required and must not be empty.");

        if (quantity < 0)
            throw new BookValidationException("Quantity cannot be negative.");

        if (categoryId is < 0 or > 6)
            throw new BookValidationException("The selected category is not valid.");

        if (!IsValidId(id))
            throw new ArgumentException("Book id must be a 24-character hexadecimal ObjectId.", nameof(id));

        DateTimeOffset? effectiveInactiveDate = active ? null : inactiveDate ?? now;

        return new Book(
            id.ToLowerInvariant(),
            title,
            author,
            categoryId,
            quantity,
            description,
            language,
            active,
            effectiveInactiveDate,
            publisher,
            publisherDate);
    }

    public Book ApplyStockChange(BookStockChange change) => this with
    {
        Quantity = change.Quantity ?? Quantity,
        Active = change.Active ?? Active,
        InactiveDate = change.Active is null ? InactiveDate : change.InactiveDate
    };

    internal static Book Restore(
        string id,
        string? title,
        string? author,
        int categoryId,
        int quantity,
        string? description,
        string? language,
        bool active,
        DateTimeOffset? inactiveDate,
        string? publisher,
        DateTimeOffset? publisherDate) =>
        new(
            id,
            title ?? string.Empty,
            author,
            categoryId,
            quantity,
            description,
            language,
            active,
            inactiveDate,
            publisher,
            publisherDate);

    public static bool IsValidId(string? id) =>
        id is { Length: 24 } && id.All(Uri.IsHexDigit);
}

public sealed class BookValidationException(string message) : Exception(message);
