namespace ApexPerformance.API.Shared.DataTransferObjects;

public record PersonDataDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? FullName,
    string? Address = null,
    string? Phone = null,
    string? Email = null,
    // Profile picture link (see UserPictures), null when there is none.
    string? PictureUrl = null
);