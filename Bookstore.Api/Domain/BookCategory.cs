namespace Bookstore.Api.Domain;

public sealed record BookCategory(int Id, string Name)
{
    public static IReadOnlyList<BookCategory> All { get; } = Array.AsReadOnly(
    [
        new BookCategory(1, "Fiction"),
        new BookCategory(2, "Non-Fiction"),
        new BookCategory(3, "Science"),
        new BookCategory(4, "Biography"),
        new BookCategory(5, "Children"),
        new BookCategory(6, "Programming")
    ]);

    public static bool TryGetId(string? name, out int id)
    {
        var category = All.FirstOrDefault(
            category => string.Equals(category.Name, name, StringComparison.OrdinalIgnoreCase));
        id = category?.Id ?? 0;
        return category is not null;
    }
}
