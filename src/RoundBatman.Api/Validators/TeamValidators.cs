using FluentValidation;
using RoundBatman.Api.Dtos;

namespace RoundBatman.Api.Validators;

public class CreateTeamDtoValidator : AbstractValidator<CreateTeamDto>
{
    public CreateTeamDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Team name is required.").MaximumLength(100);
    }
}

public class UpdateTeamDtoValidator : AbstractValidator<UpdateTeamDto>
{
    public UpdateTeamDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Team name is required.").MaximumLength(100);
    }
}
