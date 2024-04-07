namespace SMTPServer.Common;

public static class Constants
{
    public static readonly string DATA_DIRECTORY = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                                                        + "/gary";
    
    public static readonly string PATH = $"{DATA_DIRECTORY}/.emails.json";
    public static readonly string ATTACHMENTS_DIR = $"{DATA_DIRECTORY}/attachments/";
}