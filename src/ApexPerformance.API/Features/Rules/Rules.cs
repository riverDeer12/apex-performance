using ApexPerformance.API.Constants;
using ApexPerformance.API.Database;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Features.Trainings;
using ApexPerformance.API.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ApexPerformance.API.Features.Rules;

public record RulesResponse(
    // RulesDocuments value.
    string Document,
    // Version the client has to accept.
    string Version,
    // PDF of the rules, to open or download.
    string DocumentUrl,
    // Only clients with individual trainings have to accept these rules.
    bool IsRequired,
    // The current version is accepted.
    bool IsAccepted,
    string? AcceptedVersion,
    DateTimeOffset? AcceptedAt);

public record AcceptRulesRequest(
    // Version shown to the client, it must be the current one.
    string Version,
    // Parent or guardian accepting for a minor client.
    string? GuardianName,
    // Parent's approval for a client aged 14-17 to come and leave alone.
    bool IndependentTravelApproved);

public record RulesAcceptanceResponse(
    Guid ClientId,
    string ClientName,
    string Plan,
    bool IsRequired,
    string? Version,
    bool IsCurrent,
    DateTimeOffset? AcceptedAt,
    string? GuardianName,
    bool IndependentTravelApproved);

internal static class RulesHelper
{
    public static string DocumentUrl(IConfiguration configuration)
        => (configuration["WebAppUrl"] ?? string.Empty).TrimEnd('/') + RulesDocuments.IndividualCoachingPath;

    /// <summary>
    /// Rules are asked for only once they are published ("Rules:Published" set to true, on production
    /// after the legal review); on test and development right away, so they can be tried out.
    /// </summary>
    public static bool IsPublished(IConfiguration configuration, IHostEnvironment environment)
        => configuration.GetValue<bool?>("Rules:Published") ??
           (environment.IsDevelopment() || environment.IsEnvironment("Test"));

    public static bool IsRequired(Client client, bool isPublished)
        => isPublished && client.Plan == ClientPlans.PrivateCoaching;

    public static Task<RulesAcceptance?> LastAcceptance(ApexPerformanceContext context, Guid clientId,
        CancellationToken cancellationToken)
        => context.RulesAcceptances
            .AsNoTracking()
            .Where(x => x.ClientId == clientId && x.Document == RulesDocuments.IndividualCoaching)
            .OrderByDescending(x => x.AcceptedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public static RulesResponse ToResponse(Client client, RulesAcceptance? acceptance, IConfiguration configuration,
        IHostEnvironment environment)
        => new(RulesDocuments.IndividualCoaching, RulesDocuments.IndividualCoachingVersion,
            DocumentUrl(configuration), IsRequired(client, IsPublished(configuration, environment)),
            acceptance?.Version == RulesDocuments.IndividualCoachingVersion,
            acceptance?.Version, acceptance?.AcceptedAt);
}

/// <summary>
/// Current rules for the logged client and whether they accepted them.
/// The app asks a client who has to accept and hasn't yet.
/// </summary>
public class GetRulesEndpoint : EndpointWithoutRequest<RulesResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public GetRulesEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        IConfiguration configuration, IWebHostEnvironment environment)
    {
        _context = context;
        _currentUserService = currentUserService;
        _configuration = configuration;
        _environment = environment;
    }

    public override void Configure()
    {
        Get("api/rules");
        Roles(UserRoles.Client);
        Options(x => x.WithTags("Rules"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var client = await _context.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId, cancellationToken);

        if (client is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        var acceptance = await RulesHelper.LastAcceptance(_context, client.Id, cancellationToken);

        await SendAsync(RulesHelper.ToResponse(client, acceptance, _configuration, _environment), cancellation: cancellationToken);
    }
}

/// <summary>
/// The logged client accepts the current rules. The version, time, IP address
/// and device are kept as a record; accepting again the same version keeps the
/// first record.
/// </summary>
public class AcceptRulesEndpoint : Endpoint<AcceptRulesRequest, RulesResponse>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public AcceptRulesEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        IConfiguration configuration, IWebHostEnvironment environment)
    {
        _context = context;
        _currentUserService = currentUserService;
        _configuration = configuration;
        _environment = environment;
    }

    public override void Configure()
    {
        Post("api/rules/accept");
        Roles(UserRoles.Client);
        Options(x => x.WithTags("Rules"));
    }

    public override async Task HandleAsync(AcceptRulesRequest request, CancellationToken cancellationToken)
    {
        // The app showed an older version than the current one, it has to show the new one first.
        if (request.Version != RulesDocuments.IndividualCoachingVersion)
            ThrowError(ErrorCodes.NotValid);

        var client = await _context.Clients
            .FirstOrDefaultAsync(x => x.UserId == _currentUserService.UserId, cancellationToken);

        if (client is null)
            ThrowError(ErrorCodes.NotFound, StatusCodes.Status404NotFound);

        var acceptance = await RulesHelper.LastAcceptance(_context, client.Id, cancellationToken);

        if (acceptance?.Version != RulesDocuments.IndividualCoachingVersion)
        {
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
            var guardianName = request.GuardianName?.Trim();

            acceptance = new RulesAcceptance
            {
                ClientId = client.Id,
                UserId = _currentUserService.UserId,
                Document = RulesDocuments.IndividualCoaching,
                Version = RulesDocuments.IndividualCoachingVersion,
                AcceptedAt = DateTimeOffset.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = userAgent.Length > 300 ? userAgent[..300] : userAgent,
                GuardianName = string.IsNullOrEmpty(guardianName) ? null : guardianName,
                // Approval of the parent, so only together with a parent accepting.
                IndependentTravelApproved = !string.IsNullOrEmpty(guardianName) && request.IndependentTravelApproved
            };

            _context.RulesAcceptances.Add(acceptance);

            await _context.SaveChangesAsync(cancellationToken);
        }

        await SendAsync(RulesHelper.ToResponse(client, acceptance, _configuration, _environment), cancellation: cancellationToken);
    }
}

public sealed class AcceptRulesValidator : Validator<AcceptRulesRequest>
{
    public AcceptRulesValidator()
    {
        RuleFor(x => x.Version).NotEmpty().WithMessage(ErrorCodes.Required);
        RuleFor(x => x.GuardianName).MaximumLength(200).WithMessage(ErrorCodes.NotValid);
    }
}

/// <summary>
/// Which clients accepted the rules: all clients for administrators,
/// their own clients for coaches. The latest acceptance of each client.
/// </summary>
public class GetRulesAcceptancesEndpoint : EndpointWithoutRequest<List<RulesAcceptanceResponse>>
{
    private readonly ApexPerformanceContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public GetRulesAcceptancesEndpoint(ApexPerformanceContext context, ICurrentUserService currentUserService,
        IConfiguration configuration, IWebHostEnvironment environment)
    {
        _context = context;
        _currentUserService = currentUserService;
        _configuration = configuration;
        _environment = environment;
    }

    public override void Configure()
    {
        Get("api/rules/acceptances");
        Roles(UserRoles.SuperAdmin, UserRoles.Administrator, UserRoles.Coach);
        Options(x => x.WithTags("Rules"));
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var clients = _context.Clients.AsNoTracking();

        if (!TrainingAccess.IsAdministrator(_currentUserService))
        {
            clients = clients.Where(client => _context.CoachClients.Any(x =>
                x.ClientId == client.Id && x.Coach.UserId == _currentUserService.UserId));
        }

        var rows = await clients
            .Select(client => new
            {
                Client = client,
                Acceptance = _context.RulesAcceptances
                    .Where(x => x.ClientId == client.Id && x.Document == RulesDocuments.IndividualCoaching)
                    .OrderByDescending(x => x.AcceptedAt)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var isPublished = RulesHelper.IsPublished(_configuration, _environment);

        await SendAsync(rows
                .Select(x => new RulesAcceptanceResponse(
                    x.Client.Id,
                    x.Client.FullName,
                    x.Client.Plan,
                    RulesHelper.IsRequired(x.Client, isPublished),
                    x.Acceptance?.Version,
                    x.Acceptance?.Version == RulesDocuments.IndividualCoachingVersion,
                    x.Acceptance?.AcceptedAt,
                    x.Acceptance?.GuardianName,
                    x.Acceptance?.IndependentTravelApproved ?? false))
                .OrderBy(x => x.ClientName)
                .ToList(),
            cancellation: cancellationToken);
    }
}
