using Bookstore.Api.Application;
using Bookstore.Api.Contracts;
using Bookstore.Api.Domain;
using Bookstore.Api.Validation;
using FluentValidation;

namespace Bookstore.Api.Endpoints;

public static class BookEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var books = endpoints.MapGroup("/api/v1/books")
            .WithTags("Books")
            .WithDescription("Authentication: none. Book catalogue operations are currently available to any reachable client.");

        books.MapGet("", ListAsync)
            .WithName("ListBooks")
            .WithSummary("List or search the book catalogue")
            .WithDescription("Returns active and inactive books, optionally filtered by category, exact title, or exact author. Results are limited to one page; page size is at most 100.")
            .Produces<IReadOnlyList<BookResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        books.MapGet("/{id}", GetByIdAsync)
            .WithName("GetBookById")
            .WithSummary("Get a book by its identifier")
            .WithDescription("Returns the complete book record for a 24-character MongoDB ObjectId.")
            .Produces<BookResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        books.MapPost("", CreateAsync)
            .WithName("CreateBook")
            .WithSummary("Register a book")
            .WithDescription("Creates a book. Titles must not be empty, quantity must be non-negative, and category is an optional category name. Duplicate titles are allowed.")
            .Produces<BookResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        books.MapPatch("/{id}", UpdateAsync)
            .WithName("UpdateBookStock")
            .WithSummary("Partially update stock or availability")
            .WithDescription("Only supplied fields are changed. At least one of quantity or active is required; the complete updated book is returned.")
            .Produces<BookResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status415UnsupportedMediaType)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        endpoints.MapGet("/health", HealthAsync)
            .WithTags("Health")
            .WithName("GetHealth")
            .WithSummary("Check API and database readiness")
            .WithDescription("Authentication: none. Pings MongoDB and returns 503 if the inventory database is unavailable.")
            .Produces<HealthResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        IBookService service,
        CancellationToken cancellationToken,
        string? category = null,
        string? title = null,
        string? author = null,
        int page = 1,
        int pageSize = BookService.DEFAULT_PAGE_SIZE)
    {
        var books = await service.ListAsync(category, title, author, page, pageSize, cancellationToken);
        return TypedResults.Ok(books.Select(BookResponse.FromDomain).ToArray());
    }

    private static async Task<IResult> GetByIdAsync(
        string id,
        IBookService service,
        CancellationToken cancellationToken)
    {
        var book = await service.GetByIdAsync(id, cancellationToken);
        return book is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(BookResponse.FromDomain(book));
    }

    private static async Task<IResult> CreateAsync(
        CreateBookRequest? request,
        IValidator<CreateBookRequest> validator,
        IBookService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
            throw new BookValidationException("A request body is required.");

        await ValidateAsync(validator, request, cancellationToken);

        var command = new CreateBookCommand(
            request.Title,
            request.Author,
            request.Category,
            request.Quantity,
            request.Description,
            request.Language,
            request.Active,
            request.InactiveDate,
            request.Publisher,
            request.PublisherDate);
        var book = await service.RegisterAsync(command, cancellationToken);
        return TypedResults.Created($"/api/v1/books/{book.Id}", BookResponse.FromDomain(book));
    }

    private static async Task<IResult> UpdateAsync(
        string id,
        UpdateBookRequest? request,
        IValidator<UpdateBookRequest> validator,
        IBookService service,
        CancellationToken cancellationToken)
    {
        if (request is null)
            throw new BookValidationException("A request body is required.");

        await ValidateAsync(validator, request, cancellationToken);

        var book = await service.UpdateAsync(
            id,
            new UpdateBookCommand(request.Quantity, request.Active),
            cancellationToken);
        return book is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(BookResponse.FromDomain(book));
    }

    private static async Task<IResult> HealthAsync(
        IBookService service,
        CancellationToken cancellationToken)
    {
        var healthy = await service.IsHealthyAsync(cancellationToken);
        return healthy
            ? TypedResults.Ok(new HealthResponse("healthy"))
            : TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task ValidateAsync<TRequest>(
        IValidator<TRequest> validator,
        TRequest request,
        CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new BookValidationException(result.Errors[0].ErrorMessage);
    }
}
