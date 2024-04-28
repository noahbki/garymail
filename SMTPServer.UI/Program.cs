using SMTPServer.UI.Components;

namespace SMTPServer.UI;

public static class SMTPServerUI
{
    public static void Main(string[] args)
    {
        Start(args);
    }

    public static void Start(params string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddControllers();
        builder.WebHost.UseUrls("http://localhost:6969");

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();

        // app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.MapControllers();

        app.Run();
    }
}
