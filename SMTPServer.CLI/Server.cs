using System.Net;
using System.Net.Sockets;
using System.Text;
using SMTPServer.Common;
using SMTPServer.Common.Models;
using System.Text.RegularExpressions;
using MimeTypes;

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

        var emailModel = new EmailModel();
        
        var emailData = receivedData + remainingDataSB;
        
        var emailHeaders = emailData.Split("\r\n\r\n").FirstOrDefault();
        var parsedEmailHeaders = ParseHeaders(emailHeaders!);
        emailModel.From = parsedEmailHeaders.From;
        emailModel.To = parsedEmailHeaders.To;
        emailModel.Subject = parsedEmailHeaders.Subject;
        emailModel.ReceivedDateTime = DateTime.Now;
        
        var emailSections = ParseEmailContent(emailData);

        var textSections = emailSections.Where(x
            => x.ContentType?.StartsWith("multipart/alternative", StringComparison.OrdinalIgnoreCase) == true);

        var attachments = emailSections.Where(x => x.IsAttachment);

        emailModel.Content = textSections.FirstOrDefault()?.Content;
        foreach (var attachment in attachments)
        {
            var filename = _EmailStore.SaveAttachment(CleanAndConvertFromBase64(attachment.Content!));
            emailModel.Attachments.Add(new AttachmentModel
            {
                Filepath = filename,
                AttachmentFilename = $"attachment-{filename}{MimeTypeMap.GetExtension(attachment.ContentType)}"
            });
        }
        
        _EmailStore.AddEmail(emailModel);

        response = Responses.OK;
        return true;
    }

    private List<EmailSection> ParseEmailContent(string emailContent)
    {
        var result = new List<EmailSection>();

        var boundaryRegex = new Regex(@"boundary=(?<boundary>.+)");
        Match boundaryMatch = boundaryRegex.Match(emailContent);
        if (!boundaryMatch.Success)
        {
            return result;
        }

        var boundary = boundaryMatch.Groups["boundary"].Value;

        var sections = emailContent.Split(new[] { "--" + boundary }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var section in sections)
        {
            var contentType = GetContentType(section);
            var content = GetContent(section);
            var isAttachment = IsAttachment(contentType, section);
            
            result.Add(new EmailSection
            {
                ContentType = contentType,
                Content = content,
                IsAttachment = isAttachment
            });
        }

        return result;
    }

    private string? GetContentType(string section)
    {
        var contentTypeRegex = new Regex(@"Content-Type:\s*([^\s;]+)");
        Match match = contentTypeRegex.Match(section);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private string? GetContent(string section)
    {
        var contentRegex = new Regex(@"\r?\n\r?\n(.*)", RegexOptions.Singleline);
        Match match = contentRegex.Match(section);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }
    
    private bool IsAttachment(string? contentType, string section)
    {
        // Check content type for attachment indicators
        if (contentType != null && contentType.StartsWith("application/octet-stream"))
        {
            return true;
        }

        // Check for Content-Disposition header indicating attachment
        var contentDispositionRegex = new Regex(@"Content-Disposition:\s*attachment");
        return contentDispositionRegex.IsMatch(section);
    }

    private byte[] CleanAndConvertFromBase64(string base64)
    {
        var trimmed = base64.Split("----boundary");
        return Convert.FromBase64String(trimmed.FirstOrDefault()!);
    }

    private EmailHeaders ParseHeaders(string section)
    {
        var lines = section.Split("\r\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new EmailHeaders
        {
            To = lines.FirstOrDefault(x => x.StartsWith("To: ", StringComparison.OrdinalIgnoreCase))?["To: ".Length..],
            From = lines.FirstOrDefault(x => x.StartsWith("From: ", StringComparison.OrdinalIgnoreCase))?["From: ".Length..],
            Subject = lines.FirstOrDefault(x => x.StartsWith("Subject: ", StringComparison.OrdinalIgnoreCase))?["Subject: ".Length..]
        };
    }
}

public class EmailSection
{
    public string? ContentType { get; set; }
    public string? Content { get; set; }
    public bool IsAttachment { get; set; }
}

public class EmailHeaders
{
    public string? To { get; set; }
    public string? From { get; set; }
    public string? Subject { get; set; }
}