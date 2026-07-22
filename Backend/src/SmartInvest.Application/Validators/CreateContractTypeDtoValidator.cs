using FluentValidation;
using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Validators;

public class CreateContractTypeDtoValidator : AbstractValidator<CreateContractTypeDto>
{
    public CreateContractTypeDtoValidator()
    {
        RuleFor(x => x.ContractName).NotEmpty().WithMessage("اسم نوع العقد مطلوب").MaximumLength(150);
    }
}

public class UpdateContractTypeDtoValidator : AbstractValidator<UpdateContractTypeDto>
{
    public UpdateContractTypeDtoValidator()
    {
        RuleFor(x => x.ContractName).NotEmpty().WithMessage("اسم نوع العقد مطلوب").MaximumLength(150);
    }
}
