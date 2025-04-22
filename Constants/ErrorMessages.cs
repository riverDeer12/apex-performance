using FluentValidation.Results;

namespace ApexPerformance.API.Constants;

public static class ErrorMessages
{
    public const string NotAuthorized = nameof(NotAuthorized);
    public const string NotFound = nameof(NotFound); 
    public const string RolesNotProvided = nameof(RolesNotProvided);
    public const string SavingError = nameof(SavingError);
    public const string UnauthorizedAction = nameof(UnauthorizedAction);
    public const string AlreadyExists = nameof(UnauthorizedAction);
}