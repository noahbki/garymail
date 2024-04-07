namespace SMTPServer.Common.Models;

public class EmailModel
{
    public string? Recipient { get; set; }
    public string? Sender { get; set; }
    public string? Subject { get; set; }
    public string? Content { get; set; }
    public DateTime? ReceivedDateTime { get; set; }

    public List<AttachmentModel> Attachments { get; set; } = [];
}
