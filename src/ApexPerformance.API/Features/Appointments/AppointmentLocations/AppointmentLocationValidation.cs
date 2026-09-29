using ApexPerformance.API.Constants;
using FastEndpoints;
using FluentValidation;

namespace ApexPerformance.API.Features.Appointments.AppointmentLocations;

/// <summary>
/// Request for creating and updating
/// appointment locations.
/// </summary>
public record AppointmentLocationRequest(
    string Name,
    string? Address,
    decimal Latitude,
    decimal Longitude,
    string? GoogleMapsUrl
);

public sealed class AppointmentLocationRequestValidator : Validator<AppointmentLocationRequest>
{
    public AppointmentLocationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(ErrorCodes.Required)
            .MaximumLength(200).WithMessage(ErrorCodes.NotValid);

        RuleFor(x => x.Address).MaximumLength(300).WithMessage(ErrorCodes.NotValid);

        RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m).WithMessage(ErrorCodes.NotValid);

        RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m).WithMessage(ErrorCodes.NotValid);

        RuleFor(x => x.GoogleMapsUrl)
            .MaximumLength(500).WithMessage(ErrorCodes.NotValid)
            .Must(BeHttpUrl).WithMessage(ErrorCodes.NotValid)
            .When(x => !string.IsNullOrWhiteSpace(x.GoogleMapsUrl));
    }

    private static bool BeHttpUrl(string? url)
        => Uri.TryCreate(url?.Trim(), UriKind.Absolute, out var uri) &&
           (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}

public static class AppointmentLocationValidation
{
    public static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
