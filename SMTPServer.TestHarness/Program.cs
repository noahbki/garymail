using System.Net.Mail;

namespace SMTPServer.TestHarness;

class Program
{
    static void Main(string[] args)
    {
        var smtpClient = new SmtpClient()
        {
            Host = "localhost",
            Port = 8080,
            EnableSsl = false,
            UseDefaultCredentials = true
        };

        var message = new MailMessage("testharness@smtpserver.com", "recipient@smtpserver.com", "Violet Sky", "Grace Kelly says hi!");
        using var memoryStream = new MemoryStream();
        memoryStream.Write("anything you like"u8);
        memoryStream.Seek(0, SeekOrigin.Begin);
        
        message.Attachments.Add(new Attachment(memoryStream, "test.txt", "text/plain"));
        
        smtpClient.Send(message);
        
        Console.WriteLine("Sent");
    }
}