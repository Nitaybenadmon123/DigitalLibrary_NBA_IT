using System;
using System.Net;
using System.Net.Mail;
using System.Configuration;

public class EmailService
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly bool _enableSsl;

    public EmailService()
    {
        // שליפת ההגדרות מקובץ ה- Web.config
        _host = ConfigurationManager.AppSettings["SmtpServer"];
        _port = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
        _username = ConfigurationManager.AppSettings["SenderEmail"];
        _password = ConfigurationManager.AppSettings["SenderPassword"];
        _enableSsl = bool.Parse(ConfigurationManager.AppSettings["EnableSsl"]);
    }

    public void SendEmail(string toEmail, string subject, string body)
    {
        try
        {
            using (var client = new SmtpClient(_host, _port))
            {
                client.Credentials = new NetworkCredential(_username, _password);
                client.EnableSsl = _enableSsl;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_username),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                client.Send(mailMessage);
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Error sending email: " + ex.Message);
        }
    }
}
