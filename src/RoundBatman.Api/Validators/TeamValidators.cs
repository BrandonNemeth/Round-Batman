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

public class GenerateTeamsDtoValidator : AbstractValidator<GenerateTeamsDto>
{
    public GenerateTeamsDtoValidator()
    {
        RuleFor(x => x.Count).InclusiveBetween(1, 32)
            .WithMessage("The generated team count must be between 1 and 32.");
    }
}
