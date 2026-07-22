using FluentValidation;
using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Validators;

public class CreateProjectAssignmentDtoValidator : AbstractValidator<CreateProjectAssignmentDto>
{
    public CreateProjectAssignmentDtoValidator()
    {
        RuleFor(x => x.ContractTypeId).GreaterThan(0).WithMessage("يجب اختيار نوع العقد");
        RuleFor(x => x.ContractValue).GreaterThanOrEqualTo(0).When(x => x.ContractValue.HasValue)
            .WithMessage("قيمة العقد لا يمكن أن تكون سالبة");
        RuleFor(x => x.ExpectedEndDate).GreaterThan(x => x.ExpectedStartDate)
            .WithMessage("تاريخ الانتهاء المتوقع يجب أن يكون بعد تاريخ البدء المتوقع");
    }
}

public class UpdateProjectAssignmentDtoValidator : AbstractValidator<UpdateProjectAssignmentDto>
{
    public UpdateProjectAssignmentDtoValidator()
    {
        RuleFor(x => x.ContractTypeId).GreaterThan(0).WithMessage("يجب اختيار نوع العقد");
    }
}
