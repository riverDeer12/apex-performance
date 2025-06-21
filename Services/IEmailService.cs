using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Services;

public interface IEmailService
{
    /// <summary>
    /// Send email/emails to client email/emails
    /// when new appointment is arranged.
    /// </summary>
    /// <param name="clients">Clients that need to get notification
    /// about new appointment.</param>
    /// <param name="appointment">Appointment that needs to be sent.</param>
    /// <returns></returns>
    void SendAppointmentEmailToClients(List<Client> clients, Appointment appointment);

    /// <summary>
    /// Send email to coach for new
    /// appointment request.
    /// </summary>
    /// <param name="coachEmail">Coach email value.</param>
    /// <returns></returns>
    void SendAppointmentToCoach(string coachEmail);

    /// <summary>
    /// Send email to user for resetting
    /// the password. It contains link with
    /// which user is redirected to page for
    /// setting new password.
    /// </summary>
    /// <param name="user">User that needs to reset email.</param>
    /// <returns></returns>
    void SendResetPasswordEmail(User user);

    /// <summary>
    /// Send email with notification
    /// that new client is registered in the system.
    /// </summary>
    /// <param name="client">Client that needs to get credentials.</param>
    /// <param name="password">Password value.</param>
    /// <returns></returns>
    void SendClientCredentialsEmail(Client client, string password);
}