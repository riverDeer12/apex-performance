using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using MailKit.Net.Smtp;
using MimeKit;

namespace ApexPerformance.API.Services.Implementation;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly IAuthenticationService _authenticationService;

    public EmailService(IConfiguration configuration, IAuthenticationService authenticationService)
    {
        _configuration = configuration;
        _authenticationService = authenticationService;
    }

    public void SendAppointmentStatus(List<Client> clients, Appointment appointment, TimeSlot timeSlot)
    {
        foreach (var client in clients)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_configuration["MailConfiguration::FromName"],
                _configuration["MailConfiguration::FromAddress"]));

            message.To.Add(new MailboxAddress(client.FullName, client.Email));

            message.Subject = appointment.AppointmentStatus.Name + " Appointment";

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates",
                "ClientAppointmentStatusEmail.html");

            var html = File.ReadAllText(templatePath);

            html = html.Replace("{{ClientFullName}}", client.FullName);

            html = html.Replace("{{Description}}",
                "Your Appointment has been " + appointment.AppointmentStatus.Name + ".");
            
            html = html.Replace("{{AppointmentTime}}", timeSlot.Name);
            
            message.Body = new TextPart("html") { Text = html };

            ConnectToMailServer(message);
        }
    }

    public void SendAppointmentRequestEmail(List<Coach> coaches, List<Client> clients, Appointment appointment,
        TimeSlot timeSlot)
    {
        foreach (var coach in coaches)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_configuration["MailConfiguration::FromName"],
                _configuration["MailConfiguration::FromAddress"]));

            message.To.Add(new MailboxAddress(coach.FullName, coach.Email));

            message.Subject = "You Have New Appointment Request";

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates",
                "NewAppointmentRequestEmail.html");

            var html = File.ReadAllText(templatePath);

            var clientsNames = string.Join(",", clients.Select(x => x.FullName));

            html = html.Replace("{{CoachFullName}}", coach.FullName);

            html = html.Replace("{{Clients}}", clientsNames);

            html = html.Replace("{{AppointmentTime}}", timeSlot.Name);

            var approveLink = _configuration["WebAppUrl"] + "/new-appointment-request/approve";

            var declineLink = _configuration["WebAppUrl"] + "/new-appointment-request/decline";

            html = html.Replace("{{ApproveLink}}", approveLink);

            html = html.Replace("{{DeclineLink}}", declineLink);

            message.Body = new TextPart("html") { Text = html };

            ConnectToMailServer(message);
        }
    }

    public void SendResetPasswordEmail(User user)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_configuration["MailConfiguration::FromName"],
            _configuration["MailConfiguration::FromAddress"]));

        message.To.Add(new MailboxAddress(user.UserName, user.Email));

        message.Subject = "Password Changed Successfully!";

        var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ResetUserPasswordEmail.html");

        var html = File.ReadAllText(templatePath);

        html = html.Replace("{{Username}}", user.UserName);

        message.Body = new TextPart("html") { Text = html };

        ConnectToMailServer(message);
    }

    public void SendForgotPasswordEmail(User user, string token)
    {
        var resetPasswordLink = $"{_configuration["WebAppUrl"]}/authentication/reset-password?token={token}";

        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_configuration["MailConfiguration::FromName"],
            _configuration["MailConfiguration::FromAddress"]));

        message.To.Add(new MailboxAddress(user.UserName, user.Email));

        message.Subject = "Forgot Password Link";

        var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ForgotPasswordEmail.html");

        var html = File.ReadAllText(templatePath);

        html = html.Replace("{{Username}}", user.UserName);

        html = html.Replace("{{ResetPasswordLink}}", resetPasswordLink);

        message.Body = new TextPart("html") { Text = html };

        ConnectToMailServer(message);
    }

    public async void SendCredentialsEmail(User user, string password, string jwtToken)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_configuration["MailConfiguration::FromName"],
            _configuration["MailConfiguration::FromAddress"]));

        message.To.Add(new MailboxAddress(user.UserName, user.Email));

        message.Subject = "You Have Been Registered to Apex Performance";

        var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "CredentialsEmail.html");

        var html = await File.ReadAllTextAsync(templatePath);

        html = html.Replace("{{Username}}", user.UserName);

        html = html.Replace("{{Password}}", password);

        html = html.Replace("{{LoginLink}}",
            _configuration["WebAppUrl"] + "/authentication/mail-confirmation?token=" + jwtToken);

        message.Body = new TextPart("html") { Text = html };

        ConnectToMailServer(message);
    }

    public void SendCancelationRequest(Client client, Appointment appointment)
    {
        var coaches = appointment.Coaches.Select(x => x.Coach).ToList();

        foreach (var coach in coaches)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_configuration["MailConfiguration::FromName"],
                _configuration["MailConfiguration::FromAddress"]));

            message.To.Add(new MailboxAddress(coach.FullName, coach.Email));

            message.Subject = "You Have New Cancelation Request from: " + client.FullName;

            var templatePath =
                Path.Combine(Directory.GetCurrentDirectory(), "Templates", "CancelationRequestEmail.html");

            var clientsNames = string.Join(",", appointment.Clients.Select(x => x.Client.FullName));

            var html = File.ReadAllText(templatePath);

            html = html.Replace("{{ClientFullName}}", client.FullName);

            html = html.Replace("{{CoachFullName}}", coach.FullName);

            html = html.Replace("{{Clients}}", clientsNames);

            html = html.Replace("{{Day}}", appointment.StartTime.ToString("dd.MM.yyyy"));

            html = html.Replace("{{StartTime}}", appointment.StartTime.ToString("HH:mm"));

            html = html.Replace("{{EndTime}}", appointment.EndTime.ToString("HH:mm"));

            var approveLink = _configuration["WebAppUrl"] + "/cancelation-request/approve";

            var declineLink = _configuration["WebAppUrl"] + "/cancelation-request/decline";

            html = html.Replace("{{ApproveLink}}", approveLink);

            html = html.Replace("{{DeclineLink}}", declineLink);

            message.Body = new TextPart("html") { Text = html };

            ConnectToMailServer(message);
        }
    }

    public void SendWeekAppointmentsSchedule(Client client, string schedule)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_configuration["MailConfiguration::FromName"],
            _configuration["MailConfiguration::FromAddress"]));

        message.To.Add(new MailboxAddress(client.FullName, client.Email));

        message.Subject = "Next Week Schedule " + client.FullName;

        var templatePath =
            Path.Combine(Directory.GetCurrentDirectory(), "Templates", "WeekAppointmentsSchedule.html");

        var html = File.ReadAllText(templatePath);

        html = html.Replace("{{Username}}", client.FullName);

        html = html.Replace("{{Schedule}}", schedule);

        message.Body = new TextPart("html") { Text = html };

        ConnectToMailServer(message);
    }

    private void ConnectToMailServer(MimeMessage message)
    {
        using var smtpClient = new SmtpClient();

        try
        {
            smtpClient.Connect(_configuration["MailConfiguration::Host"],
                int.Parse(_configuration["MailConfiguration::Port"]!),
                MailKit.Security.SecureSocketOptions.StartTls);
            smtpClient.Authenticate(_configuration["MailConfiguration::Username"],
                _configuration["MailConfiguration::Password"]);
            smtpClient.Send(message);
            smtpClient.Disconnect(true);
            Console.WriteLine("Email sent successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
        }
    }
}