using Bookstore.Api.Contracts;
using Bookstore.Api.Domain;
using FluentValidation;

namespace Bookstore.Api.Validation;

public sealed class CreateBookRequestValidator : AbstractValidator<CreateBookRequest>
{
    public CreateBookRequestValidator()
    {
        RuleFor(request => request.Title)
            .NotEmpty()
            .WithMessage("Title is required and must not be empty.");

        RuleFor(request => request.Quantity)
            .Must(quantity => quantity is null or >= 0)
            .WithMessage("Quantity cannot be negative.");

        RuleFor(request => request.Category)
            .Must(category => category is null || BookCategory.TryGetId(category, out _))
            .WithMessage("Category must be one of the supported category names.");
    }
}
