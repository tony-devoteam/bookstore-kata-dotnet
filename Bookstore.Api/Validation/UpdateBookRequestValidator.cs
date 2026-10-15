using Bookstore.Api.Contracts;
using FluentValidation;

namespace Bookstore.Api.Validation;

public sealed class UpdateBookRequestValidator : AbstractValidator<UpdateBookRequest>
{
    public UpdateBookRequestValidator()
    {
        RuleFor(request => request)
            .Must(request => request.Quantity is not null || request.Active is not null)
            .WithMessage("At least one of quantity or active must be provided.");

        RuleFor(request => request.Quantity)
            .Must(quantity => quantity is null or >= 0)
            .WithMessage("Quantity cannot be negative.");
    }
}
