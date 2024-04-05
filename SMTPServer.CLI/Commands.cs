namespace SMTPServer.CLI;

public static class Commands
{
    public const string HELO = "HELO";
    public const string EHLO = "EHLO";
    
    public const string QUIT = "QUIT";
    
    public const string MAIL_FROM = "MAIL FROM";
    public const string RCPT_TO = "RCPT TO";
    public const string DATA = "DATA";
}