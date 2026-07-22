using FluentValidation;
using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Validators;

public class CreateContractorDtoValidator : AbstractValidator<CreateContractorDto>
{
    public CreateContractorDtoValidator()
    {
        RuleFor(x => x.ContractorName).NotEmpty().WithMessage("اسم المقاول مطلوب").MaximumLength(200);
        RuleFor(x => x.NationalIdOrCommercialRegister).NotEmpty().WithMessage("الرقم القومي أو السجل التجاري مطلوب");
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("رقم الهاتف مطلوب");
        RuleFor(x => x.Email).NotEmpty().WithMessage("البريد الإلكتروني مطلوب")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة");
        RuleFor(x => x.UserName).NotEmpty().WithMessage("اسم المستخدم مطلوب").MinimumLength(3);
        RuleFor(x => x.Password).NotEmpty().WithMessage("كلمة المرور مطلوبة").MinimumLength(6);
    }
}

public class UpdateContractorDtoValidator : AbstractValidator<UpdateContractorDto>
{
    public UpdateContractorDtoValidator()
    {
        RuleFor(x => x.ContractorName).NotEmpty().WithMessage("اسم المقاول مطلوب").MaximumLength(200);
        RuleFor(x => x.NationalIdOrCommercialRegister).NotEmpty().WithMessage("الرقم القومي أو السجل التجاري مطلوب");
        RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("رقم الهاتف مطلوب");
        RuleFor(x => x.Email).NotEmpty().WithMessage("البريد الإلكتروني مطلوب")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة");
    }
}
