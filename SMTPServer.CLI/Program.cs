using System.Text.Json;

namespace SMTPServer.CLI;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            if (args[0].Equals("list", StringComparison.OrdinalIgnoreCase))
            {
                // TODO
                var emailStore = new EmailStore();
                emailStore.Init();
                foreach (var email in emailStore.Emails)
                {
                    Console.WriteLine("-----------------------------START-----------------------------");
                    Console.WriteLine($"To: {email.To}");
                    Console.WriteLine($"From: {email.From}");
                    Console.WriteLine($"Subject: {email.Subject}");
                    Console.WriteLine($"Received: {email.ReceivedDateTime}");
                    Console.WriteLine($"Attachment Count: {email.Attachments.Count}");
                    Console.WriteLine();
                    Console.WriteLine(email.Content);
                    Console.WriteLine("------------------------------END-----------------------------");
                }
                
            }
        }
        else
        {
            var server = new Server();
            server.Start();            
        }
    }
}