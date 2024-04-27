using System.Net;
using System.Net.Sockets;
using System.Text;
using SMTPServer.Common.Models;
using MimeKit;

namespace SMTPServer.CLI;

public class Server
{
    private readonly string _IP_ADDRESS = "127.0.0.1";
    private readonly int _PORT  = 8080;

    private Dictionary<TcpClient, bool> _ClientState = new();
    private readonly EmailStore _EmailStore = new();

    public void Start()
    {
        TcpListener? server = null;
        _EmailStore.Init();
        try
        {
            server = new TcpListener(IPAddress.Parse(_IP_ADDRESS), _PORT);
            server.Start();
            
            Console.WriteLine($"Listening on {_IP_ADDRESS}:{_PORT}");

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

            string? response = null;
            try
            {
                if (_ClientState[client])
                {
                    if (ProcessData(data, stream, out var x))
                    {
                        _ClientState[client] = false;
                        response = x;
                    }
                }
                else
                {
                    response = ProcessCommand(data, client);
                }
            }
            catch (Exception ex)
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

    private bool ProcessData(string receivedData, NetworkStream stream, out string response)
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

        var emailModel = new EmailModel
        {
            UID = Guid.NewGuid().ToString() 
        };
        
        var emailData = receivedData + remainingDataSB;

        using var memoryStream = new MemoryStream();
        memoryStream.Write(Encoding.UTF8.GetBytes(emailData));
        memoryStream.Seek(0, SeekOrigin.Begin);

        var mimeMessage = MimeMessage.Load(ParserOptions.Default, memoryStream);

        emailModel.To = mimeMessage.To.Mailboxes.ToList().ConvertAll(x => x.Address);
        emailModel.Cc = mimeMessage.Cc.Mailboxes.ToList().ConvertAll(x => x.Address);
        emailModel.From = mimeMessage.From.Mailboxes.FirstOrDefault()?.Address;
        emailModel.Subject = mimeMessage.Subject;
        emailModel.ReceivedDateTime = mimeMessage.Date.Date;
        emailModel.Content = mimeMessage.TextBody;
        emailModel.Attachments = [];
        foreach (var attachment in mimeMessage.Attachments)
        {
            using var attachmentStream = new MemoryStream();
            if (attachment is MimePart)
                ((MimePart) attachment).Content.DecodeTo(attachmentStream);
            else 
                ((MessagePart) attachment).Message.WriteTo(attachmentStream);

            var filenameOnDisk = _EmailStore.SaveAttachment(attachmentStream.ToArray());
            var filename = attachment.ContentDisposition.FileName;
            
            if (string.IsNullOrEmpty(filename))
            {
                MimeKit.MimeTypes.TryGetExtension(attachment.ContentType.MimeType, out var extension);
                filename = filenameOnDisk + extension;
            }
            
            emailModel.Attachments.Add(new AttachmentModel
            {
                AttachmentFilename = filename,
                Filepath = filenameOnDisk
            });
        }
        
        _EmailStore.AddEmail(emailModel);

        response = Responses.OK;
        return true;
    }
}
