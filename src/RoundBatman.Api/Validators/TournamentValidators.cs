using FluentValidation;
using RoundBatman.Api.Dtos;

namespace RoundBatman.Api.Validators;

public class TournamentFormatDtoValidator : AbstractValidator<TournamentFormatDto>
{
    public TournamentFormatDtoValidator()
    {
        RuleFor(x => x.Type).IsInEnum().WithMessage("Format type must be ROUND_ROBIN or NFL.");
        RuleFor(x => x.NumberOfGroups).GreaterThan(0).WithMessage("Number of groups must be at least 1.");
        RuleFor(x => x.MaxTeamsPerGroup).GreaterThanOrEqualTo(2).WithMessage("Max teams per group must be at least 2.");
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
            .GreaterThan(0)
            .When(x => x.NumberOfGroups.HasValue)
            .WithMessage("Number of groups must be at least 1.");

        RuleFor(x => x.MaxTeamsPerGroup)
            .GreaterThanOrEqualTo(2)
            .When(x => x.MaxTeamsPerGroup.HasValue)
            .WithMessage("Max teams per group must be at least 2.");

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

        RuleFor(x => x.Format)
            .SetValidator(new PatchTournamentFormatDtoValidator())
            .When(x => x.Format is not null);
    }
}
