namespace Bookstore.Api.Domain;

public sealed record BookStockChange(int? Quantity, bool? Active, DateTimeOffset? InactiveDate)
{
    public static BookStockChange Create(int? quantity, bool? active, DateTimeOffset now)
    {
        if (quantity is null && active is null)
            throw new BookValidationException("At least one of quantity or active must be provided.");

        if (quantity < 0)
            throw new BookValidationException("Quantity cannot be negative.");

        DateTimeOffset? inactiveDate = active switch
        {
            false => now,
            true => null,
            _ => null
        };

        return new BookStockChange(quantity, active, inactiveDate);
    }
}
