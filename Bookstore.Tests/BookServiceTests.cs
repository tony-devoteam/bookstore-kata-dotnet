using Bookstore.Api.Application;
using Bookstore.Api.Domain;
using Moq;
using Xunit;

namespace Bookstore.Tests;

public sealed class BookServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 15, 12, 30, 0, TimeSpan.Zero);
    private const string BOOK_ID = "507f1f77bcf86cd799439011";

    [Fact]
    public async Task RegisterAsync_WhenOptionalFieldsAreOmitted_UsesDomainDefaults()
    {
        var repository = new Mock<IBookRepository>();
        repository.Setup(store => store.InsertAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book book, CancellationToken _) => book);
        var service = CreateService(repository.Object);

        var book = await service.RegisterAsync(
            new CreateBookCommand("Minimal", null, null, null, null, null, null, null, null, null),
            CancellationToken.None);

        Assert.Equal(BOOK_ID, book.Id);
        Assert.Equal("Minimal", book.Title);
        Assert.Equal(0, book.CategoryId);
        Assert.Equal(0, book.Quantity);
        Assert.True(book.Active);
        Assert.Null(book.InactiveDate);
    }

    [Fact]
    public async Task RegisterAsync_WhenBookIsInactive_SetsInactiveDateFromClock()
    {
        var repository = new Mock<IBookRepository>();
        repository.Setup(store => store.InsertAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book book, CancellationToken _) => book);
        var service = CreateService(repository.Object);

        var book = await service.RegisterAsync(
            new CreateBookCommand("Withdrawn", null, null, null, null, null, false, null, null, null),
            CancellationToken.None);

        Assert.False(book.Active);
        Assert.Equal(Now, book.InactiveDate);
    }

    [Fact]
    public async Task UpdateAsync_WhenAvailabilityIsOmitted_PreservesCurrentAvailability()
    {
        var existing = Book.Register(
            BOOK_ID, "Test Driven Development", "Kent Beck", 6, 3, "A book about TDD", "English",
            true, null, "Addison-Wesley", Now.AddYears(-24), Now);
        var repository = new Mock<IBookRepository>();
        repository.Setup(store => store.UpdateStockAsync(
                BOOK_ID,
                It.IsAny<BookStockChange>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, BookStockChange change, CancellationToken _) => existing.ApplyStockChange(change));
        var service = CreateService(repository.Object);

        var updated = await service.UpdateAsync(
            BOOK_ID,
            new UpdateBookCommand(10, null),
            CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("Test Driven Development", updated.Title);
        Assert.Equal(6, updated.CategoryId);
        Assert.Equal(10, updated.Quantity);
        Assert.True(updated.Active);
        Assert.Null(updated.InactiveDate);
    }

    [Fact]
    public async Task UpdateAsync_WhenQuantityIsNegative_RejectsTheChange()
    {
        var existing = Book.Register(
            BOOK_ID, "Minimal", null, 0, 3, null, null, true, null, null, null, Now);
        var repository = new Mock<IBookRepository>();
        var service = CreateService(repository.Object);

        await Assert.ThrowsAsync<BookValidationException>(() => service.UpdateAsync(
            BOOK_ID,
            new UpdateBookCommand(-1, null),
            CancellationToken.None));
        repository.Verify(
            store => store.UpdateStockAsync(It.IsAny<string>(), It.IsAny<BookStockChange>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ListAsync_WhenFilteringAndPaging_RequestsOnlyTheSelectedPage()
    {
        var repository = new Mock<IBookRepository>();
        repository.Setup(store => store.ListAsync(It.IsAny<BookQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Book>());
        var service = CreateService(repository.Object);

        await service.ListAsync("Programming", null, null, 3, 25, CancellationToken.None);

        repository.Verify(store => store.ListAsync(
            It.Is<BookQuery>(query => query.CategoryId == 6 && query.Skip == 50 && query.Limit == 25),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenIdIsMalformed_RejectsBeforeRepositoryAccess()
    {
        var repository = new Mock<IBookRepository>();
        var service = CreateService(repository.Object);

        await Assert.ThrowsAsync<BookValidationException>(() => service.GetByIdAsync("bad-id", CancellationToken.None));

        repository.Verify(
            store => store.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static BookService CreateService(IBookRepository repository) =>
        new(repository, new FixedBookIdGenerator(BOOK_ID), new FrozenTimeProvider(Now));

    private sealed class FixedBookIdGenerator(string id) : IBookIdGenerator
    {
        public string NewId() => id;
    }

    private sealed class FrozenTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
