using ApexPerformance.API.Database.Entities;
using MailKit.Net.Smtp;
using MimeKit;

namespace ApexPerformance.API.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void SendAppointmentEmailToClients(List<Client> clients, Appointment appointment)
    {
        foreach (var client in clients)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_configuration["MailConfiguration::FromName"],
                _configuration["MailConfiguration::FromAddress"]));

            message.To.Add(new MailboxAddress(client.FullName, client.Email));

            message.Subject = "You Have New Appointment Scheduled";

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates",
                "NewClientAppointmentEmail.html");

            var html = File.ReadAllText(templatePath);

            html = html.Replace("{{ClientFullName}}", client.FullName);

            html = html.Replace("{{Type}}", appointment.AppointmentType.Name);

            html = html.Replace("{{Day}}", appointment.StartTime.ToString("dd.MM.yyyy"));

            html = html.Replace("{{StartTime}}", appointment.StartTime.ToString("HH:mm"));

            html = html.Replace("{{EndTime}}", appointment.EndTime.ToString("HH:mm"));

            message.Body = new TextPart("html") { Text = html };

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

    public void SendAppointmentToCoach(string coachEmail)
    {
        throw new NotImplementedException();
    }

    public void SendResetPasswordEmail(User user)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_configuration["MailConfiguration::FromName"],
            _configuration["MailConfiguration::FromAddress"]));

        message.To.Add(new MailboxAddress(user.UserName, user.Email));

        message.Subject = "Password Changed Successfully";

        var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ResetUserPasswordEmail.html");

        var html = File.ReadAllText(templatePath);

        html = html.Replace("{{Username}}", user.UserName);

        message.Body = new TextPart("html") { Text = html };

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

    public void SendClientCredentialsEmail(Client client, string password)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_configuration["MailConfiguration::FromName"],
            _configuration["MailConfiguration::FromAddress"]));

        message.To.Add(new MailboxAddress(client.FullName, client.Email));

        message.Subject = "You Have Been Registered to Apex Performance";

        var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "CredentialsEmail.html");

        var html = File.ReadAllText(templatePath);

        html = html.Replace("{{ClientFullName}}", client.FullName);

        html = html.Replace("{{Username}}", client.Email);

        html = html.Replace("{{Password}}", password);

        message.Body = new TextPart("html") { Text = html };

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