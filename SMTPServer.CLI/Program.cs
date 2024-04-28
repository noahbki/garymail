using SMTPServer.UI;

namespace SMTPServer.CLI;

static class Program
{
    static void Main(string[] args)
    {
        Task.Run(() =>
        {
            SMTPServerUI.Start();
        });
        
        var server = new Server();
        server.Start();
    }
}