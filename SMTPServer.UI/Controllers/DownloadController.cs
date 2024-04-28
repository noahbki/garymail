using Microsoft.AspNetCore.Mvc;
using SMTPServer.CLI;
using SMTPServer.Common;

namespace SMTPServer.UI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DownloadController : Controller
{

    [HttpGet("[action]")]
    public IActionResult Get()
    {
        if (!HttpContext.Request.Query.TryGetValue("emailUID", out var emailUID))
            return BadRequest("bad param emailuid");
        if (!HttpContext.Request.Query.TryGetValue("index", out var index))
            return BadRequest("bad param index");

        var emailStore = new EmailStore();
        emailStore.Init();
        var emailModel = emailStore.Emails.FirstOrDefault(x => x.UID == emailUID);
        if (emailModel is null)
            return BadRequest("emailModel is null");

        int.TryParse(index, out var indexInt);
        var attachmentModel = emailModel.Attachments[indexInt];
    
        var filestream = new FileStream(Constants.ATTACHMENTS_DIR + attachmentModel.Filepath, 
            FileMode.Open, FileAccess.Read);

        var result = File(filestream, "application/octet-stream", attachmentModel.AttachmentFilename);

        return result;
    }
}