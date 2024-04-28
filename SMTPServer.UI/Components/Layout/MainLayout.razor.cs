using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SMTPServer.UI.Components.Layout;

public partial class MainLayout
{
    [Inject] private IJSRuntime _JS { get; set; }
    
    public async Task ToggleDarkModeClickedAsync()
    {
        AppState.UseDarkMode = !AppState.UseDarkMode;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            // FIXME: Queue up JS Invocations and iterate over every Render cycle.
            await _JS.InvokeVoidAsync("toggleDarkMode", AppState.UseDarkMode);
        }
    }
}