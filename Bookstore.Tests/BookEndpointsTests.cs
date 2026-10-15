using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Bookstore.Tests;

public sealed class BookEndpointsTests
{
    [Fact]
    public async Task ListBooks_WhenCatalogueIsEmpty_ReturnsEmptyArray()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/books");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetOpenApi_WhenRequested_ReturnsGeneratedDocument()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/v1/books", document);
        Assert.Contains("/health", document);
        Assert.Contains("Authentication: none", document);
        Assert.Contains("Required, non-empty book title", document);
    }

    [Fact]
    public async Task GetScalar_WhenDevelopment_ReturnsInteractiveDocumentation()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/scalar/v1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetOpenApi_WhenProduction_ReturnsDocumentWithoutUi()
    {
        using var factory = new BookstoreApiFactory(environmentName: "Production");
        using var client = factory.CreateClient();

        using var specification = await client.GetAsync("/openapi/v1.json");
        using var ui = await client.GetAsync("/scalar/v1");

        Assert.Equal(HttpStatusCode.OK, specification.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, ui.StatusCode);
    }

    [Fact]
    public async Task CreateBook_WhenTitleIsMissing_ReturnsProblemDetails()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/v1/books", new { author = "Nobody" });

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBook_WhenIdIsMalformed_ReturnsProblemDetails()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/books/not-an-object-id");

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ListBooks_WhenCategoryIsUnknown_ReturnsProblemDetails()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/books?category=Poetry");

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBook_WhenRequestUsesDefaults_ReturnsCreatedBookAndLocation()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/v1/books", new { title = "Minimal" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var book = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = book.GetProperty("id").GetString();
        Assert.Matches("^[0-9a-f]{24}$", id!);
        Assert.Equal($"/api/v1/books/{id}", response.Headers.Location?.OriginalString);
        Assert.Equal("Minimal", book.GetProperty("title").GetString());
        Assert.Equal(0, book.GetProperty("categoryId").GetInt32());
        Assert.Equal(0, book.GetProperty("quantity").GetInt32());
        Assert.True(book.GetProperty("active").GetBoolean());
        Assert.False(book.TryGetProperty("inactiveDate", out _));
    }

    [Fact]
    public async Task CreateBook_WhenInactive_ReturnsAutomaticInactiveDate()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/books",
            new { title = "Withdrawn", active = false });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var book = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(book.GetProperty("active").GetBoolean());
        Assert.True(book.TryGetProperty("inactiveDate", out _));
    }

    [Fact]
    public async Task CreateBook_WhenQuantityIsNegative_ReturnsProblemDetails()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/books",
            new { title = "Invalid Stock", quantity = -1 });

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBook_WhenCategoryIsUnknown_ReturnsProblemDetails()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/books",
            new { title = "Unknown Genre", category = "Poetry" });

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBook_WhenPublicationDateHasNoOffset_ReturnsProblemDetails()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/books",
            new { title = "Date Without Zone", publisherDate = "2002-11-08T00:00:00" });

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBook_WhenPublicationDateHasOffset_PreservesOffsetInResponse()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/books",
            new { title = "Zoned Date", publisherDate = "2002-11-08T10:00:00+02:00" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var book = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.EndsWith("+02:00", book.GetProperty("publisherDate").GetString());
    }

    [Fact]
    public async Task UpdateBook_WhenOnlyQuantityIsSent_PreservesAvailabilityAndBookDetails()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();
        var id = await CreateBookAsync(client, "Test Driven Development", "Programming", 3);

        using var response = await client.PatchAsJsonAsync($"/api/v1/books/{id}", new { quantity = 10 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var book = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Test Driven Development", book.GetProperty("title").GetString());
        Assert.Equal(6, book.GetProperty("categoryId").GetInt32());
        Assert.Equal(10, book.GetProperty("quantity").GetInt32());
        Assert.True(book.GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task UpdateBook_WhenDeactivated_SetsInactiveDateAndPreservesQuantity()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();
        var id = await CreateBookAsync(client, "Dune", "Fiction", 7);

        using var response = await client.PatchAsJsonAsync($"/api/v1/books/{id}", new { active = false });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var book = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(7, book.GetProperty("quantity").GetInt32());
        Assert.False(book.GetProperty("active").GetBoolean());
        Assert.True(book.TryGetProperty("inactiveDate", out _));
    }

    [Fact]
    public async Task UpdateBook_WhenReactivated_ClearsInactiveDate()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();
        using var created = await client.PostAsJsonAsync(
            "/api/v1/books",
            new { title = "Withdrawn", active = false });
        var createdBook = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = createdBook.GetProperty("id").GetString();

        using var response = await client.PatchAsJsonAsync($"/api/v1/books/{id}", new { active = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var book = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(book.GetProperty("active").GetBoolean());
        Assert.False(book.TryGetProperty("inactiveDate", out _));
    }

    [Fact]
    public async Task UpdateBook_WhenNoFieldsAreSent_ReturnsProblemDetails()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();
        var id = await CreateBookAsync(client, "Dune", "Fiction", 7);

        using var response = await client.PatchAsJsonAsync($"/api/v1/books/{id}", new { });

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBook_WhenIdDoesNotExist_ReturnsProblemDetailsNotFound()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/books/000000000000000000000001");

        await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateBook_WhenIdDoesNotExist_ReturnsProblemDetailsNotFound()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.PatchAsJsonAsync(
            "/api/v1/books/000000000000000000000001",
            new { quantity = 3 });

        await AssertProblemDetailsAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ListBooks_WhenFilteredByCategory_ReturnsOnlyMatchingBooks()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();
        await CreateBookAsync(client, "Test Driven Development", "Programming", 3);
        await CreateBookAsync(client, "Dune", "Fiction", 5);

        using var response = await client.GetAsync("/api/v1/books?category=programming");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var books = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.Single(books!);
        Assert.Equal("Test Driven Development", books![0].GetProperty("title").GetString());
    }

    [Fact]
    public async Task ListBooks_WhenFilteredByAuthor_ReturnsMatchingBooks()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();
        using var created = await client.PostAsJsonAsync(
            "/api/v1/books",
            new { title = "Test Driven Development", author = "Kent Beck" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        await CreateBookAsync(client, "Dune", "Fiction", 5);

        using var response = await client.GetAsync("/api/v1/books?author=Kent%20Beck");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var books = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        Assert.Single(books!);
        Assert.Equal("Kent Beck", books![0].GetProperty("author").GetString());
    }

    [Fact]
    public async Task ListBooks_WhenPageSizeExceedsLimit_ReturnsProblemDetails()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/books?pageSize=101");

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBook_WhenJsonIsMalformed_ReturnsProblemDetails()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();
        using var content = new StringContent("{\"title\":", System.Text.Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/v1/books", content);

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBook_WhenContentTypeIsUnsupported_ReturnsUnsupportedMediaTypeProblem()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();
        using var content = new StringContent("{\"title\":\"Book\"}", System.Text.Encoding.UTF8, "text/plain");

        using var response = await client.PostAsync("/api/v1/books", content);

        await AssertProblemDetailsAsync(response, HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task GetHealth_WhenRepositoryIsAvailable_ReturnsHealthy()
    {
        using var factory = new BookstoreApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var health = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("healthy", health.GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetHealth_WhenRepositoryIsUnavailable_ReturnsServiceUnavailableProblem()
    {
        using var factory = new BookstoreApiFactory(databaseIsHealthy: false);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/health");

        await AssertProblemDetailsAsync(response, HttpStatusCode.ServiceUnavailable);
    }

    private static async Task<string> CreateBookAsync(
        HttpClient client,
        string title,
        string category,
        int quantity)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/books",
            new { title, category, quantity });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var book = await response.Content.ReadFromJsonAsync<JsonElement>();
        return book.GetProperty("id").GetString()!;
    }

    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var problem = document.RootElement;
        Assert.True(problem.TryGetProperty("type", out _));
        Assert.True(problem.TryGetProperty("title", out _));
        Assert.Equal((int)expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.True(problem.TryGetProperty("detail", out _));
        Assert.StartsWith("urn:trace:", problem.GetProperty("instance").GetString());
        Assert.True(problem.TryGetProperty("traceId", out var traceId));
        Assert.Equal(traceId.GetString(), response.Headers.GetValues("X-Trace-Id").Single());
    }
}
