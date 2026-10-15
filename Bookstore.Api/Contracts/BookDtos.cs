using Bookstore.Api.Domain;

namespace Bookstore.Api.Contracts;

/// <summary>Fields accepted when registering a book in the catalogue.</summary>
/// <param name="Title">Required, non-empty book title. Duplicate titles are permitted.</param>
/// <param name="Author">Optional author name.</param>
/// <param name="Category">Optional category name; matching ignores capitalization.</param>
/// <param name="Quantity">Initial non-negative stock quantity; defaults to zero.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Language">Optional language.</param>
/// <param name="Active">Initial availability; defaults to true.</param>
/// <param name="InactiveDate">Optional date with an explicit ISO 8601 offset. An inactive book without one is dated automatically.</param>
/// <param name="Publisher">Optional publisher name.</param>
/// <param name="PublisherDate">Optional publication date with an explicit ISO 8601 offset.</param>
public sealed record CreateBookRequest(
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

/// <summary>Optional stock and availability fields to change on an existing book.</summary>
/// <param name="Quantity">New non-negative stock quantity. Omit to preserve the current quantity.</param>
/// <param name="Active">New availability. Omit to preserve the current availability.</param>
public sealed record UpdateBookRequest(int? Quantity, bool? Active);

/// <summary>Book information returned by the inventory API.</summary>
/// <param name="Id">System-generated 24-character MongoDB ObjectId.</param>
/// <param name="Title">Book title.</param>
/// <param name="Author">Optional author name.</param>
/// <param name="CategoryId">Numeric category id; zero means no category.</param>
/// <param name="Quantity">Current number of copies in stock.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Language">Optional language.</param>
/// <param name="Active">Whether the book is offered for sale.</param>
/// <param name="InactiveDate">UTC date when the book was deactivated, omitted while active.</param>
/// <param name="Publisher">Optional publisher name.</param>
/// <param name="PublisherDate">Optional publication date.</param>
public sealed record BookResponse(
    string Id,
    string Title,
    string? Author,
    int CategoryId,
    int Quantity,
    string? Description,
    string? Language,
    bool Active,
    DateTimeOffset? InactiveDate,
    string? Publisher,
    DateTimeOffset? PublisherDate)
{
    public static BookResponse FromDomain(Book book) => new(
        book.Id,
        book.Title,
        book.Author,
        book.CategoryId,
        book.Quantity,
        book.Description,
        book.Language,
        book.Active,
        book.InactiveDate,
        book.Publisher,
        book.PublisherDate);
}

/// <summary>Readiness state for the API and its MongoDB dependency.</summary>
public sealed record HealthResponse(string Status);
