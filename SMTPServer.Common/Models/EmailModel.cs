namespace SMTPServer.Common.Models;

public class EmailModel
{
    public string? To { get; set; }
    public string? From { get; set; }
    public string? Subject { get; set; }
    public string? Content { get; set; }
    public DateTime? ReceivedDateTime { get; set; }

    public List<AttachmentModel> Attachments { get; set; } = [];
}
