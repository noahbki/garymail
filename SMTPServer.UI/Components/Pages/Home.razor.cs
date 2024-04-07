using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using SMTPServer.CLI;
using SMTPServer.Common.Models;

namespace SMTPServer.UI.Components.Pages;

public partial class Home : ComponentBase
{
    private EmailStore _EmailStore { get; set; }
    
    private ObservableCollection<EmailModel> _Emails { get; set; }
    private int _SelectedIndex { get; set; } = 0;
    
    public Home()
    {
        _EmailStore = new EmailStore();
    }
    
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        
        _EmailStore.Init();
        _Emails = new(_EmailStore.Emails.OrderByDescending(x => x.ReceivedDateTime));
        StateHasChanged();
    }
}