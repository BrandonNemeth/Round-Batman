using FluentValidation;
using RoundBatman.Api.Dtos;

namespace RoundBatman.Api.Validators;

public class TournamentFormatDtoValidator : AbstractValidator<TournamentFormatDto>
{
    public TournamentFormatDtoValidator()
    {
        RuleFor(x => x.Type).IsInEnum().WithMessage("Format type must be ROUND_ROBIN or NFL.");
        RuleFor(x => x.NumberOfGroups).Equal(1).WithMessage("Round Robin tournaments must have exactly one group.");
        RuleFor(x => x.MaxTeamsPerGroup).GreaterThanOrEqualTo(2).WithMessage("Max teams per group must be at least 2.");
        RuleFor(x => x).Must(x => x.NumberOfGroups * x.MaxTeamsPerGroup <= 32)
            .WithMessage("A tournament cannot be configured for more than 32 teams.");
    }
}

public class CreateTournamentDtoValidator : AbstractValidator<CreateTournamentDto>
{
    public CreateTournamentDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Tournament name is required.");
        RuleFor(x => x.Format).NotNull().WithMessage("Format is required.");
        RuleFor(x => x.Format).SetValidator(new TournamentFormatDtoValidator()).When(x => x.Format is not null);
    }
}

public class UpdateTournamentDtoValidator : AbstractValidator<UpdateTournamentDto>
{
    public UpdateTournamentDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Tournament name is required.");
        RuleFor(x => x.Format).NotNull().WithMessage("Format is required.");
        RuleFor(x => x.Format).SetValidator(new TournamentFormatDtoValidator()).When(x => x.Format is not null);
    }
}

public class PatchTournamentFormatDtoValidator : AbstractValidator<PatchTournamentFormatDto>
{
    public PatchTournamentFormatDtoValidator()
    {
        RuleFor(x => x.NumberOfGroups)
            .Equal(1)
            .When(x => x.NumberOfGroups.HasValue)
            .WithMessage("Round Robin tournaments must have exactly one group.");

        RuleFor(x => x.MaxTeamsPerGroup)
            .GreaterThanOrEqualTo(2)
            .When(x => x.MaxTeamsPerGroup.HasValue)
            .WithMessage("Max teams per group must be at least 2.");
        RuleFor(x => x).Must(x => !x.NumberOfGroups.HasValue || !x.MaxTeamsPerGroup.HasValue ||
                                   x.NumberOfGroups.Value * x.MaxTeamsPerGroup.Value <= 32)
            .WithMessage("A tournament cannot be configured for more than 32 teams.");

        RuleFor(x => x.Type)
            .IsInEnum()
            .When(x => x.Type.HasValue)
            .WithMessage("Format type must be ROUND_ROBIN or NFL.");
    }
}

public class PatchTournamentDtoValidator : AbstractValidator<PatchTournamentDto>
{
    public PatchTournamentDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .When(x => x.Name is not null)
            .WithMessage("Tournament name cannot be empty when provided.");

        RuleFor(x => x.Format!)
            .SetValidator(new PatchTournamentFormatDtoValidator())
            .When(x => x.Format is not null);
    }
}
