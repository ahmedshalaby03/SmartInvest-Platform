using FluentValidation;
using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Validators;

public class CreateFinancialYearDtoValidator : AbstractValidator<CreateFinancialYearDto>
{
    public CreateFinancialYearDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("اسم السنة المالية مطلوب").MaximumLength(50);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate)
            .WithMessage("تاريخ النهاية يجب أن يكون بعد تاريخ البداية");

        RuleFor(x => x.Budget).GreaterThanOrEqualTo(0).When(x => x.Budget.HasValue)
            .WithMessage("الموازنة لا يمكن أن تكون سالبة");
    }
}

public class UpdateFinancialYearDtoValidator : AbstractValidator<UpdateFinancialYearDto>
{
    public UpdateFinancialYearDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("اسم السنة المالية مطلوب").MaximumLength(50);
        RuleFor(x => x.EndDate).GreaterThan(x => x.StartDate)
            .WithMessage("تاريخ النهاية يجب أن يكون بعد تاريخ البداية");

        RuleFor(x => x.Budget).GreaterThanOrEqualTo(0).When(x => x.Budget.HasValue)
            .WithMessage("الموازنة لا يمكن أن تكون سالبة");
    }
}
