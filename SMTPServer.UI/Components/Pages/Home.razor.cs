using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SMTPServer.CLI;
using SMTPServer.Common;
using SMTPServer.Common.Models;

namespace SMTPServer.UI.Components.Pages;

public partial class Home : ComponentBase
{
    [Inject]
    private IJSRuntime _JS { get; set; }
    
    private EmailStore _EmailStore { get; set; }
    
    public ObservableCollection<EmailModel> _Emails { get; set; }
    private int _SelectedIndex { get; set; } = 0;
    
    public Home()
    {
        _EmailStore = new EmailStore();
    }
    
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        
        _EmailStore.Init();
        LoadEmails();
        StateHasChanged();
        
        _EmailStore.WatchForChanges(() =>
        {
            LoadEmails();
            InvokeAsync(StateHasChanged);
        });
    }

    private void RefreshClicked()
    {
        _SelectedIndex = 0;
        LoadEmails();
        StateHasChanged();
    }

    private void SelectEmail(int index)
    {
        _SelectedIndex = index;
    }

    private void DeleteEmailClicked()
    {
        _SelectedIndex = 0;
        _EmailStore.DeleteEmail(_Emails[_SelectedIndex].UID);
        LoadEmails();
        StateHasChanged();
    }
    
    private void DeleteAllClicked()
    {
        _EmailStore.DeleteAll();
        LoadEmails();
        StateHasChanged();
    }

    private void LoadEmails()
    {
        _EmailStore.Init();
        _Emails = new(_EmailStore.Emails.OrderByDescending(x => x.ReceivedDateTime));
    }

    private async void DownloadAttachment(string attachmentFilename)
    {
        var email = _Emails[_SelectedIndex];
        await _JS.InvokeVoidAsync("downloadFileFromStream", email.UID, email.Attachments.FindIndex(x => x.Filepath == attachmentFilename));
    }
}