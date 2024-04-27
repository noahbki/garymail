using System.Text.Json;

using SMTPServer.Common;
using SMTPServer.Common.Models;

namespace SMTPServer.CLI;

public class EmailStore
{
    public List<EmailModel> Emails { get; set; } = [];

    public void Init()
    {
        Directory.CreateDirectory(Constants.DATA_DIRECTORY);
        using var file = File.Open(Constants.PATH, FileMode.OpenOrCreate, FileAccess.Read);
        using var reader = new StreamReader(file);
        var fileContents = reader.ReadToEnd();
        try
        {
            Emails = JsonSerializer.Deserialize<List<EmailModel>>(fileContents) ?? [];
        }
        catch
        {
            Emails = [];
        }

        Directory.CreateDirectory(Constants.ATTACHMENTS_DIR);
    }
    
    public void AddEmail(EmailModel email)
    {
        Init();
        Emails.Add(email);
        var fileContents = JsonSerializer.Serialize(Emails);
        using var file = File.Open(Constants.PATH, FileMode.Create, FileAccess.Write);
        using var streamWriter = new StreamWriter(file);
        streamWriter.Write(fileContents);
    }

    public void DeleteEmail(string uid)
    {
        Init();
        var index = Emails.FindIndex(x => x.UID == uid);
        Emails.RemoveAt(index);
        using var file = File.Open(Constants.PATH, FileMode.Create, FileAccess.Write);
        using var streamWriter = new StreamWriter(file);
        streamWriter.Write(JsonSerializer.Serialize(Emails));
    }

    public void DeleteAll()
    {
        Emails = [];
        using var file = File.Open(Constants.PATH, FileMode.Create, FileAccess.Write);
        using var streamWriter = new StreamWriter(file);
        streamWriter.Write(JsonSerializer.Serialize(Emails));
    }

    public string SaveAttachment(byte[] bytes)
    {
        var filename = Guid.NewGuid().ToString();
        using var file = File.Open(Constants.ATTACHMENTS_DIR + filename, FileMode.CreateNew);
        file.Write(bytes, 0, bytes.Length);

        return filename;
    }
}
