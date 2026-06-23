using FluentValidation;
using STEM.Application.Dtos.Classes;

namespace STEM.Application.Validators;

public class CreateClassRequestValidator : AbstractValidator<CreateClassRequest>
{
    public CreateClassRequestValidator()
    {
        RuleFor(x => x.ClassCode)
            .NotEmpty().WithMessage("Mã lớp không được để trống.")
            .MaximumLength(50).WithMessage("Mã lớp không được vượt quá 50 ký tự.");

        RuleFor(x => x.CourseId)
            .GreaterThan(0).WithMessage("ID khóa học không hợp lệ.");

        RuleFor(x => x.TeacherId)
            .GreaterThan(0).WithMessage("ID giáo viên không hợp lệ.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Ngày bắt đầu không được để trống.")
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date).WithMessage("Ngày bắt đầu phải là hôm nay hoặc trong tương lai.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("Ngày kết thúc không được để trống.")
            .GreaterThan(x => x.StartDate).WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");
    }
}

public class UpdateClassRequestValidator : AbstractValidator<UpdateClassRequest>
{
    public UpdateClassRequestValidator()
    {
        RuleFor(x => x.ClassCode)
            .NotEmpty().WithMessage("Mã lớp không được để trống.")
            .MaximumLength(50).WithMessage("Mã lớp không được vượt quá 50 ký tự.");

        RuleFor(x => x.CourseId)
            .GreaterThan(0).WithMessage("ID khóa học không hợp lệ.");

        RuleFor(x => x.TeacherId)
            .GreaterThan(0).WithMessage("ID giáo viên không hợp lệ.");

        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("Ngày bắt đầu không được để trống.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("Ngày kết thúc không được để trống.")
            .GreaterThan(x => x.StartDate).WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");
    }
}
