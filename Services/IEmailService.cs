using ApexPerformance.API.Database.Entities;

namespace ApexPerformance.API.Services;

public interface IEmailService
{
    /// <summary>
    /// Send email/emails to client email/emails
    /// when a new appointment is arranged.
    /// </summary>
    /// <param name="clients">Clients that need to get notification
    /// about a new appointment.</param>
    /// <param name="appointment">Appointment that needs to be sent.</param>
    /// <returns></returns>
    void SendAppointmentStatus(List<Client> clients, Appointment appointment);

    /// <summary>
    /// Send email to coach for new
    /// appointment request.
    /// </summary>
    /// <param name="coaches">Coaches that need to get email notification.</param>
    /// <param name="appointment">Appointment from request.</param>
    /// <returns></returns>
    void SendAppointmentRequestEmail(List<Coach> coaches, Appointment appointment);

    /// <summary>
    /// Send email to user for resetting
    /// the password. It contains a link with
    /// which the user is redirected to the page for setting a
    ///  new password.
    /// </summary>
    /// <param name="user">User that needs to reset email.</param>
    /// <param name="token">Value of JWT token.</param>
    /// <returns></returns>
    void SendForgotPasswordEmail(User user, string token);    
    
    /// <summary>
    /// Send email to user, so it is
    /// notified about changing password.
    /// </summary>
    /// <param name="user">User that needs to reset email.</param>
    /// <returns></returns>
    void SendResetPasswordEmail(User user);

    /// <summary>
    /// Send email with notification
    /// when a new user is registered in the system.
    /// </summary>
    /// <param name="user">User account that needs to get credentials.</param>
    /// <param name="password">Password value.</param>
    /// <param name="jwtToken">JWT token value.</param>
    /// <returns></returns>
    void SendCredentialsEmail(User user, string password, string jwtToken);

    /// <summary>
    /// Send email to coaches which are connected to this
    /// appointment about client's request for cancellation.
    /// </summary>
    /// <param name="client">Client that sends cancellation request.</param>
    /// <param name="appointment">Appointment that client wants to cancel.</param>
    void SendCancelationRequest(Client client, Appointment appointment);
}