using System.Text;
using System.Text.Json;

namespace SMTPServer.CLI;

public class EmailStore
{
    private static readonly string BASE_DIR = AppDomain.CurrentDomain.BaseDirectory;
    
    private static readonly string PATH = $"{BASE_DIR}/.emails.json";
    public static readonly string ATTACHMENTS_DIR = $"{BASE_DIR}/attachments/";
    
    public List<EmailModel> Emails { get; set; } = [];

    public void Init()
    {
        using var file = File.Open(PATH, FileMode.OpenOrCreate, FileAccess.Read);
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

        Directory.CreateDirectory(ATTACHMENTS_DIR);
    }

    public void AddEmail(EmailModel email)
    {
        Emails.Add(email);
        var fileContents = JsonSerializer.Serialize(Emails);
        using var file = File.Open(PATH, FileMode.Create, FileAccess.Write);
        using var streamWriter = new StreamWriter(file);
        streamWriter.Write(fileContents);
    }

    public string SaveAttachment(byte[] bytes)
    {
        var filename = Guid.NewGuid().ToString();
        using var file = File.Open(ATTACHMENTS_DIR + filename, FileMode.CreateNew);
        file.Write(bytes, 0, bytes.Length);

        return filename;
    }
}
