using FluentValidation;
using RoundBatman.Api.Dtos;

namespace RoundBatman.Api.Validators;

public class CreateMatchDtoValidator : AbstractValidator<CreateMatchDto>
{
    public CreateMatchDtoValidator()
    {
        RuleFor(x => x.HomeTeamId).NotEmpty().WithMessage("Home team ID is required.");
        RuleFor(x => x.VisitorTeamId).NotEmpty().WithMessage("Visitor team ID is required.");
    }
}

public class UpdateScoreDtoValidator : AbstractValidator<UpdateScoreDto>
{
    public UpdateScoreDtoValidator()
    {
        RuleFor(x => x.HomeTeamScore).GreaterThanOrEqualTo(0).WithMessage("Home team score cannot be negative.");
        RuleFor(x => x.VisitorTeamScore).GreaterThanOrEqualTo(0).WithMessage("Visitor team score cannot be negative.");
    }
}
