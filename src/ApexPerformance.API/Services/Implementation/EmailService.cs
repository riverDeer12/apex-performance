using ApexPerformance.API.Constants;
using ApexPerformance.API.Database.Entities;
using ApexPerformance.API.Database.Entities.Catalog;
using ApexPerformance.API.Features.Payments;
using ApexPerformance.API.Services.Interfaces;
using MimeKit;
using Stripe.Checkout;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

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

            message.From.Add(new MailboxAddress(_configuration["MailConfiguration:FromName"],
                _configuration["MailConfiguration:FromAddress"]));

            message.To.Add(new MailboxAddress(client.FullName, client.Email));

            message.Subject = appointment.AppointmentStatus.Name + " Appointment";

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates",
                "ClientAppointmentStatusEmail.html");

            var html = File.ReadAllText(templatePath);

            var statusColor = appointment.AppointmentStatus.Name switch
            {
                BusinessStatuses.Approved => "#2edc59",
                BusinessStatuses.Pending => "#ffc107",
                BusinessStatuses.InProgress => "#ffc107",
                _ => "#f36464"
            };

            html = html.Replace("{{ClientFullName}}", client.FullName);

            html = html.Replace("{{Description}}",
                "Your Appointment has been <span style=\"color:" + statusColor + ";text-decoration: underline;\">"
                + appointment.AppointmentStatus.Name +
                "</span>.");

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

            message.From.Add(new MailboxAddress(_configuration["MailConfiguration:FromName"],
                _configuration["MailConfiguration:FromAddress"]));

            message.To.Add(new MailboxAddress(coach.FullName, coach.Email));

            message.Subject = "You Have New Appointment Request";

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates",
                "NewAppointmentRequestEmail.html");

            var html = File.ReadAllText(templatePath);

            var clientsNames = string.Join(",", clients.Select(x => x.FullName));

            html = html.Replace("{{CoachFullName}}", coach.FullName);

            html = html.Replace("{{Clients}}", clientsNames);

            html = html.Replace("{{AppointmentTime}}", timeSlot.Name);

            html = html.Replace("{{DashboardLink}}", _configuration["WebAppUrl"] + "/admin/dashboard");

            message.Body = new TextPart("html") { Text = html };

            ConnectToMailServer(message);
        }
    }

    public void SendResetPasswordEmail(User user)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_configuration["MailConfiguration:FromName"],
            _configuration["MailConfiguration:FromAddress"]));

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

        message.From.Add(new MailboxAddress(_configuration["MailConfiguration:FromName"],
            _configuration["MailConfiguration:FromAddress"]));

        message.To.Add(new MailboxAddress(user.UserName, user.Email));

        message.Subject = "Forgot Password Link";

        var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ForgotPasswordEmail.html");

        var html = File.ReadAllText(templatePath);

        html = html.Replace("{{Username}}", user.UserName);

        html = html.Replace("{{ResetPasswordLink}}", resetPasswordLink);

        message.Body = new TextPart("html") { Text = html };

        ConnectToMailServer(message);
    }

    public void SendWelcomeEmail(User user, string token)
    {
        var setPasswordLink = $"{_configuration["WebAppUrl"]}/authentication/reset-password?token={token}";

        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_configuration["MailConfiguration:FromName"],
            _configuration["MailConfiguration:FromAddress"]));

        message.To.Add(new MailboxAddress(user.UserName, user.Email));

        message.Subject = "Welcome to Apex Performance";

        var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "WelcomeEmail.html");

        var html = File.ReadAllText(templatePath);

        html = html.Replace("{{Username}}", user.UserName);

        html = html.Replace("{{SetPasswordLink}}", setPasswordLink);

        message.Body = new TextPart("html") { Text = html };

        ConnectToMailServer(message);
    }

    public async void SendCredentialsEmail(User user, string password, string jwtToken)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_configuration["MailConfiguration:FromName"],
            _configuration["MailConfiguration:FromAddress"]));

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

            message.From.Add(new MailboxAddress(_configuration["MailConfiguration:FromName"],
                _configuration["MailConfiguration:FromAddress"]));

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

            html = html.Replace("{{DashboardLink}}", _configuration["WebAppUrl"] + "/admin/dashboard");

            message.Body = new TextPart("html") { Text = html };

            ConnectToMailServer(message);
        }
    }

    public void SendJoinRequest(Client client, Appointment appointment)
    {
        var coaches = appointment.Coaches.Select(x => x.Coach).ToList();

        foreach (var coach in coaches)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_configuration["MailConfiguration:FromName"],
                _configuration["MailConfiguration:FromAddress"]));

            message.To.Add(new MailboxAddress(coach.FullName, coach.Email));

            message.Subject = "You Have New Join Request from: " + client.FullName;

            var templatePath =
                Path.Combine(Directory.GetCurrentDirectory(), "Templates", "JoinRequestEmail.html");

            var clientsNames = string.Join(",", appointment.Clients.Select(x => x.Client.FullName));

            var html = File.ReadAllText(templatePath);

            html = html.Replace("{{ClientFullName}}", client.FullName);

            html = html.Replace("{{CoachFullName}}", coach.FullName);

            html = html.Replace("{{Clients}}", clientsNames);

            html = html.Replace("{{Day}}", appointment.StartTime.ToString("dd.MM.yyyy"));

            html = html.Replace("{{StartTime}}", appointment.StartTime.ToString("HH:mm"));

            html = html.Replace("{{EndTime}}", appointment.EndTime.ToString("HH:mm"));

            html = html.Replace("{{DashboardLink}}", _configuration["WebAppUrl"] + "/admin/dashboard");

            message.Body = new TextPart("html") { Text = html };

            ConnectToMailServer(message);
        }
    }

    public void SendWeekAppointmentsSchedule(Client client, string schedule)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_configuration["MailConfiguration:FromName"],
            _configuration["MailConfiguration:FromAddress"]));

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

    public void SendLowCreditsAlertEmail(Client client)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_configuration["MailConfiguration:FromName"],
            _configuration["MailConfiguration:FromAddress"]));

        message.To.Add(new MailboxAddress(client.FullName, client.Email));

        message.Subject = "Low Credits Alert – Only 1 Credit Left";

        var templatePath =
            Path.Combine(Directory.GetCurrentDirectory(), "Templates", "LowCreditsAlertEmail.html");

        var html = File.ReadAllText(templatePath);

        html = html.Replace("{{ClientFullName}}", client.FullName);

        message.Body = new TextPart("html") { Text = html };

        ConnectToMailServer(message);
    }

    public void SendNoCreditsEmail(Client client)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(_configuration["MailConfiguration:FromName"],
            _configuration["MailConfiguration:FromAddress"]));

        message.To.Add(new MailboxAddress(client.FullName, client.Email));

        message.Subject = "No Credits Available";

        var templatePath =
            Path.Combine(Directory.GetCurrentDirectory(), "Templates", "NoCreditsEmail.html");

        var html = File.ReadAllText(templatePath);

        html = html.Replace("{{ClientFullName}}", client.FullName);

        message.Body = new TextPart("html") { Text = html };

        ConnectToMailServer(message);
    }

    public void SendJoinedAppointmentEmail(Appointment appointment, List<Client> appointmentClients,
        Client joiningClient)
    {
        foreach (var client in appointmentClients)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_configuration["MailConfiguration:FromName"],
                _configuration["MailConfiguration:FromAddress"]));

            message.To.Add(new MailboxAddress(client.FullName, client.Email));

            message.Subject = joiningClient.FullName + " has joined your appointment";

            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates",
                "ClientJoinedAppointmentEmail.html");

            var html = File.ReadAllText(templatePath);

            html = html.Replace("{{ClientFullName}}", client.FullName);

            html = html.Replace("{{JoiningClientFullName}}", joiningClient.FullName);

            html = html.Replace("{{AppointmentTime}}", appointment.TimeSlot.Name);

            message.Body = new TextPart("html") { Text = html };

            ConnectToMailServer(message);
        }
    }

    public void SendBoxNowPdfLabel(string contactEmail, string parcelNumber, Stream pdfLabelStream)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress("ReViv Plus",
            _configuration["MailConfiguration:FromAddress"]));

        message.To.Add(new MailboxAddress(contactEmail, contactEmail));

        message.Subject = "BoxNow PDF naljepnica za narudžbu: " + parcelNumber;

        var builder = new BodyBuilder
        {
            TextBody = "BoxNow PDF naljepnica je u privitku."
        };

        builder.Attachments.Add(
            fileName: parcelNumber + "_label.pdf",
            stream: pdfLabelStream,
            contentType: new ContentType("application", "pdf")
        );
        
        message.Body = builder.ToMessageBody();

        ConnectToMailServer(message);
    }

    public void SendFiscalizationReminderEmail(string contactEmail, Session checkoutSession)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress("ReViv Plus",
            _configuration["MailConfiguration:FromAddress"]));

        message.To.Add(new MailboxAddress(contactEmail, contactEmail));

        message.Subject = "Potrebna ručna fiskalizacija za transakciju: " + checkoutSession.Id;

        var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates",
            "FiscalizationReminderEmail.html");

        var html = File.ReadAllText(templatePath);

        html = html.Replace("{{ClientFullName}}", checkoutSession.CustomerDetails.Name);
        html = html.Replace("{{CheckoutSessionId}}", checkoutSession.Id);
        html = html.Replace("{{OrderDate}}", checkoutSession.Created.ToString("dd.MM.yyyy"));
        html = html.Replace("{{TotalAmount}}", (checkoutSession.AmountTotal / 100m).ToString());
        html = html.Replace("{{Currency}}", checkoutSession.Currency.ToUpper());
        html = html.Replace("{{PaymentStatus}}", checkoutSession.PaymentStatus);
        html = html.Replace("{{PaymentMethod}}", checkoutSession.PaymentMethodCollection);
        
        var address = checkoutSession.CustomerDetails?.Address;
        
        html = html.Replace("{{BillingName}}",
            checkoutSession.CustomerDetails?.Name ?? string.Empty);

        html = html.Replace("{{BillingLine1}}",
            address?.Line1 ?? string.Empty);

        html = html.Replace("{{BillingLine2}}",
            address?.Line2 ?? string.Empty);

        html = html.Replace("{{BillingPostalCode}}",
            address?.PostalCode ?? string.Empty);

        html = html.Replace("{{BillingCity}}",
            address?.City ?? string.Empty);

        html = html.Replace("{{BillingCountry}}",
            address?.Country ?? string.Empty);
        
        message.Body = new TextPart("html") { Text = html };

        ConnectToMailServer(message);
    }

    private void ConnectToMailServer(MimeMessage message)
    {
        using var smtpClient = new SmtpClient();

        try
        {
            smtpClient.Connect(_configuration["MailConfiguration:Host"],
                int.Parse(_configuration["MailConfiguration:Port"]!),
                MailKit.Security.SecureSocketOptions.StartTls);
            smtpClient.Authenticate(_configuration["MailConfiguration:Username"],
                _configuration["MailConfiguration:Password"]);
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