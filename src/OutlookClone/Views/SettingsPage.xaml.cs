using OutlookClone.Services;

namespace OutlookClone.Views;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();

        var application = Application.Current;
        var currentTheme = application?.UserAppTheme is AppTheme.Light or AppTheme.Dark
            ? application.UserAppTheme
            : application?.RequestedTheme ?? AppTheme.Light;
        ThemeSwitch.IsToggled = currentTheme == AppTheme.Dark;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var settings = await MailConnectionSettings.LoadAsync();
            ImapHostEntry.Text = settings.ImapHost;
            ImapPortEntry.Text = settings.ImapPort.ToString();
            SmtpHostEntry.Text = settings.SmtpHost;
            SmtpPortEntry.Text = settings.SmtpPort.ToString();
            UsernameEntry.Text = settings.Username;
            PasswordEntry.Text = settings.Password;
        }
        catch (Exception exception)
        {
            ConnectionStatusLabel.Text = $"Unable to load connection settings: {exception.Message}";
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void OnThemeToggled(object? sender, ToggledEventArgs e)
    {
        if (Application.Current is { } application)
            application.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
    }

    private async void OnSaveConnectionClicked(object? sender, EventArgs e)
    {
        var imapHost = ImapHostEntry.Text?.Trim() ?? string.Empty;
        var smtpHost = SmtpHostEntry.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(imapHost) || string.IsNullOrWhiteSpace(smtpHost) ||
            !ushort.TryParse(ImapPortEntry.Text, out var imapPort) || imapPort == 0 ||
            !ushort.TryParse(SmtpPortEntry.Text, out var smtpPort) || smtpPort == 0)
        {
            ConnectionStatusLabel.Text = "Enter both server hosts and valid ports (1-65535).";
            return;
        }

        try
        {
            var settings = new MailConnectionSettings(
                imapHost,
                imapPort,
                smtpHost,
                smtpPort,
                UsernameEntry.Text?.Trim() ?? string.Empty,
                PasswordEntry.Text ?? string.Empty);
            await settings.SaveAsync();
            ConnectionStatusLabel.Text = "Connection settings saved.";
        }
        catch (Exception exception)
        {
            ConnectionStatusLabel.Text = $"Unable to save connection settings: {exception.Message}";
        }
    }
}