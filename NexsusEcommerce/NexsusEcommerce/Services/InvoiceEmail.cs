using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public interface IEmailService1
{
    Task SendEmailAsync(string toEmail, string subject, string body, byte[] attachmentBytes = null, string attachmentName = null);
}

public class InvoiceEmail : IEmailService1
{
    private readonly string _smtpServer = "smtp.gmail.com";
    private readonly int _smtpPort = 587;
    private readonly string _smtpUser = "your_email@gmail.com"; // Your Gmail address
    private readonly string _smtpPass = "your_app_specific_password"; // Your Gmail app-specific password
    private readonly string _fromEmail = "your_email@gmail.com"; // Your Gmail address
    private readonly string _fromName = "Your Name"; // Your name or company name

    public async Task SendEmailAsync(string toEmail, string subject, string body, byte[] attachmentBytes = null, string attachmentName = null)
    {
        var message = new MailMessage
        {
            From = new MailAddress(_fromEmail, _fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        message.To.Add(toEmail);

        if (attachmentBytes != null && attachmentBytes.Length > 0 && !string.IsNullOrEmpty(attachmentName))
        {
            using (var attachmentStream = new MemoryStream(attachmentBytes))
            {
                var attachment = new Attachment(attachmentStream, attachmentName, "application/pdf");
                message.Attachments.Add(attachment);
            }
        }

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
