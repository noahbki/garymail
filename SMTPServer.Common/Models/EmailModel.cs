namespace SMTPServer.Common.Models;

public class EmailModel
{
    public EmailModel()
    {
        UID = Guid.NewGuid().ToString();
    }
    
    public required string UID { get; set; }
    public List<string>? To { get; set; }
    public List<string>? Cc { get; set; }
    public string? From { get; set; }
    public string? Subject { get; set; }
    public string? Content { get; set; }
    public DateTime? ReceivedDateTime { get; set; }

    public List<AttachmentModel> Attachments { get; set; } = [];
}
