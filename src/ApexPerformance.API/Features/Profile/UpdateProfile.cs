using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Profile;

public record UpdateProfileRequest(
    string Email,
    string? FirstName,
    string? LastName,
    string? Phone
);

/// <summary>
/// Update personal data of logged user. Email is kept
/// in sync on user account and client/coach data.
/// </summary>
public class UpdateProfileEndpoint : Endpoint<UpdateProfileRequest, ProfileResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProfileEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public override void Configure()
    {
        Put("api/profile");
        Options(x => x.WithTags("Profile"));
    }

    public override async Task HandleAsync(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        var user = await _context.Users
            .Include(x => x.Client)
            .Include(x => x.Coach)
            .Include(x => x.Administrator)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
            ThrowError(ErrorCodes.UserNotFound);

        var email = request.Email.Trim();

        if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase) &&
            await _context.Users.AnyAsync(x => x.Id != userId && x.Email == email, cancellationToken))
            ThrowError(ErrorCodes.EmailAlreadyExists);

        var hasPersonalData = user.Client is not null || user.Coach is not null || user.Administrator is not null;

        if (hasPersonalData && (string.IsNullOrWhiteSpace(request.FirstName) ||
                                string.IsNullOrWhiteSpace(request.LastName)))
            ThrowError(ErrorCodes.Required);

        if ((user.Client is not null || user.Coach is not null) && string.IsNullOrWhiteSpace(request.Phone))
            ThrowError(ErrorCodes.Required);

        user.Email = email;

        if (user.Client is not null)
        {
            user.Client.FirstName = request.FirstName!.Trim();
            user.Client.LastName = request.LastName!.Trim();
            user.Client.Phone = request.Phone!.Trim();
            user.Client.Email = email;
        }

        if (user.Coach is not null)
        {
            user.Coach.FirstName = request.FirstName!.Trim();
            user.Coach.LastName = request.LastName!.Trim();
            user.Coach.Phone = request.Phone!.Trim();
            user.Coach.Email = email;
        }

        if (user.Administrator is not null)
        {
            user.Administrator.FirstName = request.FirstName!.Trim();
            user.Administrator.LastName = request.LastName!.Trim();
        }

        // Marked as modified so saving without changes still succeeds.
        _context.Entry(user).State = EntityState.Modified;

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (result == 0)
            ThrowError(ErrorCodes.SavingError);

        await SendAsync((await ProfileQueries.GetProfile(_context, userId, cancellationToken))!,
            cancellation: cancellationToken);
    }
}

public sealed class UpdateProfileValidator : Validator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage(ErrorCodes.Required)
            .EmailAddress().WithMessage(ErrorCodes.NotValid)
            .MaximumLength(256).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.FirstName).MaximumLength(100).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.LastName).MaximumLength(100).WithMessage(ErrorCodes.NotValid);
        RuleFor(x => x.Phone).MaximumLength(50).WithMessage(ErrorCodes.NotValid);
    }
}
