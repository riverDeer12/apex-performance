using System.Globalization;
using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Constants;

/// <summary>
/// Device token that gets a push, with the language of the app on it.
/// </summary>
public record PushRecipient(string Token, string? Language);

/// <summary>
/// Title and body of a push notification.
/// </summary>
public record PushText(string Title, string Body);

/// <summary>
/// Push notification texts in the languages of the mobile app. Each device
/// gets them in the language its app runs in; tokens registered before the
/// app sent its language get Croatian, the app's main language.
/// </summary>
public static class PushTexts
{
    public const string Croatian = "hr";
    public const string English = "en";
    public const string Italian = "it";

    private static readonly string[] Supported = [Croatian, English, Italian];

    /// <summary>
    /// "hr", "en" or "it" from a language code like "en-US", null when not supported.
    /// </summary>
    public static string? Normalize(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return null;

        var code = language.Trim().ToLowerInvariant();
        code = code.Length > 2 ? code[..2] : code;

        return Supported.Contains(code) ? code : null;
    }

    // The message itself is not in the push, it goes through Firebase and shows on the lock screen.
    public static PushText ChatMessage(string language, string senderName) => Pick(language,
        new("Nova poruka", $"Nova poruka od: {senderName}"),
        new("New message", $"New message from {senderName}"),
        new("Nuovo messaggio", $"Nuovo messaggio da: {senderName}"));

    public static PushText BodyMeasurement(string language) => Pick(language,
        new("Novo mjerenje", "Trener je dodao novo mjerenje tijela."),
        new("New body measurement", "Your coach added a new body measurement."),
        new("Nuova misurazione", "Il tuo coach ha aggiunto una nuova misurazione corporea."));

    public static PushText Training(string language, string name) => Pick(language,
        new("Novi trening", $"Trener ti je dodao trening: {name}"),
        new("New training", $"Your coach added a training: {name}"),
        new("Nuovo allenamento", $"Il tuo coach ha aggiunto un allenamento: {name}"));

    public static PushText TrainingUpdated(string language, string name) => Pick(language,
        new("Trening ažuriran", $"Trener je ažurirao tvoj trening: {name}"),
        new("Training updated", $"Your coach updated your training: {name}"),
        new("Allenamento aggiornato", $"Il tuo coach ha aggiornato il tuo allenamento: {name}"));

    public static PushText MonthlyReview(string language) => Pick(language,
        new("Mjesečni review", "Trener je napisao review tvog mjeseca."),
        new("Monthly review", "Your coach wrote the review of your month."),
        new("Revisione mensile", "Il tuo coach ha scritto la revisione del tuo mese."));

    public static PushText ClientGoal(string language) => Pick(language,
        new("Cilj i plan", "Trener je ažurirao tvoj cilj i plan."),
        new("Goal and plan", "Your coach updated your goal and plan."),
        new("Obiettivo e piano", "Il tuo coach ha aggiornato il tuo obiettivo e piano."));

    public static PushText MonthlyReviewReminder(string language, int count) => Pick(language,
        new("Mjesečni reviewi", $"Klijenata bez mjesečnog reviewa: {count}. Napiši ih prije 1. u mjesecu."),
        new("Monthly reviews", $"{count} client(s) still have no monthly review. Write it before the 1st."),
        new("Revisioni mensili", $"Clienti senza revisione mensile: {count}. Scrivila prima del 1° del mese."));

    public static PushText CancelationRequest(string language, string clientName) => Pick(language,
        new("Zahtjev za termin", $"Novi zahtjev za otkazivanje od: {clientName}"),
        new("Appointment request", $"You have a new cancelation request from {clientName}"),
        new("Richiesta di appuntamento", $"Nuova richiesta di cancellazione da: {clientName}"));

    public static PushText JoinRequest(string language, string clientName) => Pick(language,
        new("Zahtjev za termin", $"Novi zahtjev za pridruživanje od: {clientName}"),
        new("Appointment request", $"You have a new join request from {clientName}"),
        new("Richiesta di appuntamento", $"Nuova richiesta di partecipazione da: {clientName}"));

    public static PushText AppointmentRequested(string language) => Pick(language,
        new("Zahtjev za termin", "Zatražen je novi termin."),
        new("Appointment request", "A new appointment was requested."),
        new("Richiesta di appuntamento", "È stato richiesto un nuovo appuntamento."));

    public static PushText AppointmentUpdated(string language, string status) => Pick(language,
        new("Promjena termina", $"Tvoj termin je {Status(Croatian, status)}."),
        new("Appointment update", $"Your appointment has been {Status(English, status)}."),
        new("Aggiornamento appuntamento", $"Il tuo appuntamento è stato {Status(Italian, status)}."));

    public static PushText WeeklySchedule(string language, IEnumerable<Appointment> appointments) => Pick(language,
        new("Tvoj raspored treninga za idući tjedan je spreman", ScheduleBody(Croatian, appointments)),
        new("Your weekly training schedule is ready", ScheduleBody(English, appointments)),
        new("Il tuo programma di allenamento settimanale è pronto", ScheduleBody(Italian, appointments)));

    // "Monday, 06:15:00 - 07:15:00 - type - coach", the day in the device's language.
    private static string ScheduleBody(string language, IEnumerable<Appointment> appointments)
    {
        var culture = CultureInfo.GetCultureInfo(language);

        return string.Join("\n", appointments.Select(appointment =>
        {
            var day = culture.TextInfo.ToTitleCase(culture.DateTimeFormat.GetDayName((DayOfWeek)appointment.TimeSlot.Day));
            var time = $"{appointment.TimeSlot.StartTime} - {appointment.TimeSlot.EndTime}";
            var coach = appointment.Coaches.First().Coach.FullName;
            return $"{day}, {time} - {appointment.AppointmentType.Name} - {coach}";
        }));
    }

    private static string Status(string language, string status) => (language, status) switch
    {
        (Croatian, BusinessStatuses.Approved) => "odobren",
        (Croatian, BusinessStatuses.Declined) => "odbijen",
        (Croatian, BusinessStatuses.Pending) => "na čekanju",
        (Croatian, BusinessStatuses.Canceled) => "otkazan",
        (Croatian, BusinessStatuses.InProgress) => "u tijeku",
        (Italian, BusinessStatuses.Approved) => "approvato",
        (Italian, BusinessStatuses.Declined) => "rifiutato",
        (Italian, BusinessStatuses.Pending) => "in attesa",
        (Italian, BusinessStatuses.Canceled) => "cancellato",
        (Italian, BusinessStatuses.InProgress) => "in corso",
        (English, BusinessStatuses.InProgress) => "in progress",
        _ => status.ToLowerInvariant()
    };

    private static PushText Pick(string language, PushText croatian, PushText english, PushText italian)
        => language switch
        {
            English => english,
            Italian => italian,
            _ => croatian
        };
}
