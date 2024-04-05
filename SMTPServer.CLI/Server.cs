using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Text;

namespace SMTPServer.CLI;

public class Server
{
    private string _IPAddress = "127.0.0.1";
    private int _Port  = 8080;

    private Dictionary<TcpClient, bool> _ClientState = new();
    private EmailStore _EmailStore = new();

    public void Start()
    {
        TcpListener? server = null;
        _EmailStore.Init();
        try
        {
            server = new TcpListener(IPAddress.Parse(_IPAddress), _Port);
            server.Start();
            
            Console.WriteLine($"Listening on {_IPAddress}:{_Port}");

            while (true)
            {
                var client = server.AcceptTcpClient();
                var clientThread = new Thread(() =>
                {
                    HandleClientConnection(client);
                });
                clientThread.Start();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            server?.Stop();
        }
    }

    private void HandleClientConnection(TcpClient client)
    {
        _ClientState[client] = false;
        var clientHostAddress = ((IPEndPoint?)client.Client.RemoteEndPoint)?.Address.ToString();
        Console.WriteLine("Handling client from: " + clientHostAddress);
        
        // Buffer for data
        var bytes = new byte[1024];
        var data = string.Empty;

        var stream = client.GetStream();

        var greeting = "220 SMTP Server Ready\r\n";
        var greetingBytes = Encoding.ASCII.GetBytes(greeting);
        stream.Write(greetingBytes, 0, greetingBytes.Length);

        int i;
        while ((i = stream.Read(bytes, 0, bytes.Length)) > 0)
        {
            data = Encoding.ASCII.GetString(bytes, 0, i);
            // Console.WriteLine("Received: " + data);

            string? response = null;
            try
            {
                if (_ClientState[client])
                {
                    response = ProcessData(data, stream);
                    _ClientState[client] = false;
                }
                else
                {
                    response = ProcessCommand(data, client);
                }
            }
            catch (Exception)
            {
                response = Responses.ERROR;
            }

            var responseBytes = Encoding.ASCII.GetBytes(response);
            stream.Write(responseBytes, 0, responseBytes.Length);
            // Console.WriteLine("Sent: " + response);

            if (response.StartsWith("221"))
                break;
        }

        client.Close();
        Console.WriteLine("Client disconnected.");
    }

    private string ProcessCommand(string command, TcpClient client)
    {
        if (command.StartsWith(Commands.QUIT))
            return "221 localhost Closing connection\r\n";

        if (command.StartsWith(Commands.HELO) || command.StartsWith(Commands.EHLO))
            return Responses.OK;

        if (command.StartsWith(Commands.MAIL_FROM))
            return Responses.OK;

        if (command.StartsWith(Commands.RCPT_TO))
            return Responses.OK;

        if (command.StartsWith(Commands.DATA))
        {
            _ClientState[client] = true;
            return Responses.DATA_OK;
        }
        
        return "502 Command not implemented\r\n";
    }

    private string ProcessData(string receivedData, NetworkStream stream)
    {
        var remainingDataSB = new StringBuilder();
        var buffer = new byte[1024];
        var bytesRead = 0;

        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            var data = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            remainingDataSB.Append(data);

            if (data.EndsWith("\r\n.\r\n"))
                break;
        }

        var emailModel = new EmailModel();
        
        string bodyContent = string.Empty;
        
        var emailData = receivedData + remainingDataSB;
        
        var emailSections = emailData.Split("\r\n\r\n");

        var headers = emailSections[0].Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        // TODO: Process headers.

        
        var boundarySplitter = headers.FirstOrDefault(x => x.StartsWith("boundary=", StringComparison.OrdinalIgnoreCase))?["boundary=".Length..];
        if (boundarySplitter is null)
            emailModel.Content = emailSections[1];
        else
        {
            var sections = emailData.Split("--" + boundarySplitter).Skip(1).ToList();
            for (var i = 0; i < sections.Count - 1; i++)
            {
                var section = sections[i];
                var plainText = IsSectionContentTypeTextOrHTML(section);
                
                if (plainText)
                    emailModel.Content = section.Split("\r\n\r\n").LastOrDefault()?.Trim(); 
                
                else if (IsSectionContentTypeOctet(section))
                {
                    var base64 = section.Split("\r\n\r\n").LastOrDefault()?.Trim();
                    if (base64 is null)
                        continue;
                    
                    var bytes = Convert.FromBase64String(base64);
                    var attachmentGuid = _EmailStore.SaveAttachment(bytes);
                    emailModel.Attachments.Add(new AttachmentModel()
                    {
                        Filepath = EmailStore.ATTACHMENTS_DIR + attachmentGuid,
                        AttachmentFilename = "attachment-" + attachmentGuid
                    });
                }
            }
        }
        
        _EmailStore.AddEmail(emailModel);

        return Responses.OK;
    }

    private bool IsSectionContentTypeTextOrHTML(string section)
    {
        return section.Split('\n')
            .Any(line => line.StartsWith("Content-Type: text/plain", StringComparison.OrdinalIgnoreCase) 
                         || line.StartsWith("Content-Type: text/html", StringComparison.OrdinalIgnoreCase));
    }

    private bool IsSectionContentTypeOctet(string section)
    {
        return section.Split('\n')
            .Any(line => line.StartsWith("Content-Type: application/octet-stream", StringComparison.OrdinalIgnoreCase));
    }
}