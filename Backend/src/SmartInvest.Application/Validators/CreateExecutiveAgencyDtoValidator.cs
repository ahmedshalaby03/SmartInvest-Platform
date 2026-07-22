using FluentValidation;
using SmartInvest.Application.DTOs;

namespace SmartInvest.Application.Validators;

public class CreateExecutiveAgencyDtoValidator : AbstractValidator<CreateExecutiveAgencyDto>
{
    public CreateExecutiveAgencyDtoValidator()
    {
        RuleFor(x => x.AgencyName).NotEmpty().WithMessage("اسم الجهة مطلوب").MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().WithMessage("رقم الهاتف مطلوب");
        RuleFor(x => x.Email).NotEmpty().WithMessage("البريد الإلكتروني مطلوب")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة");
        RuleFor(x => x.UserName).NotEmpty().WithMessage("اسم المستخدم مطلوب").MinimumLength(3);
        RuleFor(x => x.Password).NotEmpty().WithMessage("كلمة المرور مطلوبة").MinimumLength(6);
    }
}

public class UpdateExecutiveAgencyDtoValidator : AbstractValidator<UpdateExecutiveAgencyDto>
{
    public UpdateExecutiveAgencyDtoValidator()
    {
        RuleFor(x => x.AgencyName).NotEmpty().WithMessage("اسم الجهة مطلوب").MaximumLength(200);
        RuleFor(x => x.Phone).NotEmpty().WithMessage("رقم الهاتف مطلوب");
        RuleFor(x => x.Email).NotEmpty().WithMessage("البريد الإلكتروني مطلوب")
            .EmailAddress().WithMessage("صيغة البريد الإلكتروني غير صحيحة");
    }
}
