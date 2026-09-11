using FluentValidation;
using RoundBatman.Api.Dtos;

namespace RoundBatman.Api.Validators;

public class CreateGroupDtoValidator : AbstractValidator<CreateGroupDto>
{
    public CreateGroupDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Group name is required.");
    }
}

public class UpdateGroupDtoValidator : AbstractValidator<UpdateGroupDto>
{
    public UpdateGroupDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Group name is required.");
    }
}

public class AssignTeamsDtoValidator : AbstractValidator<AssignTeamsDto>
{
    public AssignTeamsDtoValidator()
    {
        RuleFor(x => x.TeamIds).NotEmpty().WithMessage("At least one team ID is required.");
        RuleForEach(x => x.TeamIds).NotEmpty().WithMessage("Team IDs cannot be empty strings.");
    }
}
