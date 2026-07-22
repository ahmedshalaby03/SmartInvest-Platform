using FluentValidation;
using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Validators;

public class CreateChangeRequestDtoValidator : AbstractValidator<CreateChangeRequestDto>
{
    public CreateChangeRequestDtoValidator()
    {
        RuleFor(x => x)
            .Must(x => x.RequestedContractValue.HasValue || x.RequestedExpectedEndDate.HasValue)
            .WithMessage("يجب تحديد قيمة عقد جديدة أو تاريخ انتهاء متوقع جديد على الأقل");

        RuleFor(x => x.RequestedContractValue)
            .GreaterThanOrEqualTo(0).When(x => x.RequestedContractValue.HasValue)
            .WithMessage("قيمة العقد لا يمكن أن تكون سالبة");
    }
}
