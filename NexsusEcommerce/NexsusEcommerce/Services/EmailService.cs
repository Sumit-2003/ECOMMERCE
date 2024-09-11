using NETCore.MailKit.Core;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class EmailService : IEmailService
{
    private readonly string _smtpServer = "smtp.gmail.com";
    private readonly int _smtpPort = 587;
    private readonly string _smtpUser = "dhomsesumit36@gmail.com"; // Your Gmail address
    private readonly string _smtpPass = "mpyh fvij wkpm vnax"; // Your Gmail app-specific password
    private readonly string _fromEmail = "dhomsesumit36@gmail.com"; // Your Gmail address
    private readonly string _fromName = "NexsusEcommerce"; // Your name or company name

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_fromEmail, _fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        message.To.Add(toEmail);

        try
        {
            using (var client = new SmtpClient(_smtpServer, _smtpPort))
            {
                client.Credentials = new NetworkCredential(_smtpUser, _smtpPass);
                client.EnableSsl = true;
                await client.SendMailAsync(message);
            }
        }
        catch (Exception ex)
        {
            // Log or handle the exception as needed
            throw new InvalidOperationException("Failed to send email.", ex);
        }
    }
}
