namespace SMTPServer.CLI;

public static class Responses
{
    public const string OK = "250 OK\r\n";
    public const string DATA_OK = "354 Send Data\r\n";
    
    public const string ERROR = "501 Error\r\n";
}