using System.Collections.ObjectModel;
using MailKit.Search;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Services;
using OutlookClone.Extensions;

namespace OutlookClone.Views;

public partial class InboxView : ContentView
{
    private const int PageSize = 10;
    private bool _isLoading;
    private string _activeSearchText = string.Empty;

    public InboxView()
    {
        InitializeComponent();
        BindingContext = this;
        UpdateThemeToggle();
    }

    public ObservableCollection<Models.Email> Emails { get; } = new();

    public Task RefreshAsync() => LoadPageAsync(reset: true, _activeSearchText);

    private async Task LoadPageAsync(bool reset, string? searchText = null)
    {
        if (_isLoading)
            return;

        if (reset)
        {
            _activeSearchText = searchText?.Trim() ?? string.Empty;
            Emails.Clear();
        }

        _isLoading = true;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        LoadMoreButton.IsEnabled = false;
        try
        {
            using var receiver = ImapReceiver.Create("localhost");
            IMailReader mailReader = receiver.ReadMail;
            if (!string.IsNullOrWhiteSpace(_activeSearchText))
                mailReader = mailReader.Query(SearchQuery.SubjectContains(_activeSearchText));

            var messages = await mailReader
                .Skip(Emails.Count)
                .Take(PageSize)
                .GetMimeMessagesAsync();
            var emails = messages.Convert().ToList();
            foreach (var email in emails)
                Emails.Add(email);

            LoadMoreButton.IsVisible = emails.Count == PageSize;
        }
        catch (Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Unable to load inbox: {exception}");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            _isLoading = false;
            LoadMoreButton.IsEnabled = LoadMoreButton.IsVisible;
        }
    }

    private async void OnLoadMoreClicked(object? sender, EventArgs e)
    {
        await LoadPageAsync(reset: false);
    }

    private async void OnSearchClicked(object? sender, EventArgs e)
    {
        await SearchAsync();
    }

    private async void OnSearchCompleted(object? sender, EventArgs e)
    {
        await SearchAsync();
    }

    private Task SearchAsync() => LoadPageAsync(reset: true, SearchEntry.Text);

    private async void OnComposeClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ComposePage));
    }

    private async void OnMessageDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not BindableObject { BindingContext: Models.Email email })
            return;

        await Shell.Current.GoToAsync(nameof(MessageReaderPage), new Dictionary<string, object>
        {
            [nameof(MessageReaderPage.Email)] = email
        });
    }

    private void OnThemeToggleClicked(object? sender, EventArgs e)
    {
        var application = Application.Current;
        if (application is null)
            return;

        var currentTheme = application.UserAppTheme == AppTheme.Unspecified
            ? application.RequestedTheme
            : application.UserAppTheme;
        application.UserAppTheme = currentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark;
        UpdateThemeToggle();
    }

    private void UpdateThemeToggle()
    {
        var application = Application.Current;
        var currentTheme = application?.UserAppTheme is AppTheme.Light or AppTheme.Dark
            ? application.UserAppTheme
            : application?.RequestedTheme ?? AppTheme.Light;

        ThemeToggleButton.Text = currentTheme == AppTheme.Dark ? "\u263C" : "\u263E";
        ToolTipProperties.SetText(ThemeToggleButton, currentTheme == AppTheme.Dark ? "Switch to light theme" : "Switch to dark theme");
    }
}