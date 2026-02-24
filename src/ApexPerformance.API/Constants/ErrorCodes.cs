using FluentValidation.Results;

namespace ApexPerformance.API.Constants;

public static class ErrorCodes
{
    // Validation (1000–1099)
    public const string Required = "1000";
    public const string NotValid = "1001";
    public const string DuplicatesNotAllowed = "1002";

    // Authentication / Authorization (1100–1199)
    public const string NotAuthorized = "1100";
    public const string UnauthorizedAction = "1101";
    public const string WrongUserNameOrPassword = "1102";
    public const string NotApproved = "1103";
    public const string RolesNotProvided = "1104";

    // Not Found / State (1200–1299)
    public const string NotFound = "1200";
    public const string AlreadyChanged = "1201";
    public const string UserNotFound = "1202";

    // Conflict (1300–1399)
    public const string AlreadyExists = "1300";
    public const string UsernameAlreadyExists = "1301";
    public const string EmailAlreadyExists = "1302";

    // System / Infrastructure (1400–1499)
    public const string SavingError = "1400";
    public const string ErrorSendingEmail = "1401";
}