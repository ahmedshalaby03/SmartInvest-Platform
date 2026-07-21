using FluentValidation;
using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Validators;

public class CreateProjectSpecificationDtoValidator : AbstractValidator<CreateProjectSpecificationDto>
{
    public CreateProjectSpecificationDtoValidator()
    {
        RuleFor(x => x.SpecificationName)
            .NotEmpty().WithMessage("اسم المواصفة مطلوب")
            .MaximumLength(150);

        RuleFor(x => x.SpecificationValue)
            .NotEmpty().WithMessage("قيمة المواصفة مطلوبة")
            .MaximumLength(150);

        RuleFor(x => x.Unit)
            .NotEmpty().WithMessage("الوحدة مطلوبة")
            .MaximumLength(50);
    }
}
