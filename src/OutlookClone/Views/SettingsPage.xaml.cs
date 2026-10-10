using OutlookClone.Services;

namespace OutlookClone.Views;

public partial class SettingsPage : ContentPage
{
    private bool _isLoadingSettings;

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
            _isLoadingSettings = true;
            var settings = await MailConnectionSettings.LoadAsync();
            ImapHostEntry.Text = settings.ImapHost;
            ImapPortEntry.Text = settings.ImapPort.ToString();
            SmtpHostEntry.Text = settings.SmtpHost;
            SmtpPortEntry.Text = settings.SmtpPort.ToString();
            UsernameEntry.Text = settings.Username;
            PasswordEntry.Text = settings.Password;
            PresetPicker.SelectedItem = GetKnownPreset(settings.Preset);
            UpdatePresetVisibility(settings.Preset);
        }
        catch (Exception exception)
        {
            ConnectionStatusLabel.Text = $"Unable to load connection settings: {exception.Message}";
        }
        finally
        {
            _isLoadingSettings = false;
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

    private void OnPresetChanged(object? sender, EventArgs e)
    {
        if (_isLoadingSettings || PresetPicker.SelectedItem is not string preset)
            return;

        switch (preset)
        {
            case MailConnectionSettings.LocalhostPreset:
                ImapHostEntry.Text = "localhost";
                ImapPortEntry.Text = "143";
                SmtpHostEntry.Text = "localhost";
                SmtpPortEntry.Text = "25";
                UsernameEntry.Text = string.Empty;
                PasswordEntry.Text = string.Empty;
                break;
            case MailConnectionSettings.GmailPreset:
                ImapHostEntry.Text = "imap.gmail.com";
                ImapPortEntry.Text = "993";
                SmtpHostEntry.Text = "smtp.gmail.com";
                SmtpPortEntry.Text = "587";
                break;
            case MailConnectionSettings.OutlookPreset:
                ImapHostEntry.Text = "outlook.office365.com";
                ImapPortEntry.Text = "993";
                SmtpHostEntry.Text = "smtp-mail.outlook.com";
                SmtpPortEntry.Text = "587";
                break;
            case MailConnectionSettings.DemoPreset:
                break;
        }

        UpdatePresetVisibility(preset);
    }

    private void UpdatePresetVisibility(string preset)
    {
        var isDemo = preset == MailConnectionSettings.DemoPreset;
        DemoApiLabel.IsVisible = isDemo;
        MailServerFields.IsVisible = !isDemo;
    }

    private static string GetKnownPreset(string preset) => preset switch
    {
        MailConnectionSettings.LocalhostPreset => MailConnectionSettings.LocalhostPreset,
        MailConnectionSettings.GmailPreset => MailConnectionSettings.GmailPreset,
        MailConnectionSettings.OutlookPreset => MailConnectionSettings.OutlookPreset,
        _ => MailConnectionSettings.DemoPreset
    };

    private async void OnSaveConnectionClicked(object? sender, EventArgs e)
    {
        var imapHost = ImapHostEntry.Text?.Trim() ?? string.Empty;
        var smtpHost = SmtpHostEntry.Text?.Trim() ?? string.Empty;
        var preset = PresetPicker.SelectedItem as string ?? MailConnectionSettings.LocalhostPreset;
        var isDemo = preset == MailConnectionSettings.DemoPreset;
        if (!isDemo && (string.IsNullOrWhiteSpace(imapHost) || string.IsNullOrWhiteSpace(smtpHost) ||
            !ushort.TryParse(ImapPortEntry.Text, out var parsedImapPort) || parsedImapPort == 0 ||
            !ushort.TryParse(SmtpPortEntry.Text, out var parsedSmtpPort) || parsedSmtpPort == 0))
        {
            ConnectionStatusLabel.Text = "Enter both server hosts and valid ports (1-65535).";
            return;
        }

        var imapPort = isDemo ? (ushort)143 : ushort.Parse(ImapPortEntry.Text!);
        var smtpPort = isDemo ? (ushort)25 : ushort.Parse(SmtpPortEntry.Text!);

        try
        {
            var settings = new MailConnectionSettings(
                imapHost,
                imapPort,
                smtpHost,
                smtpPort,
                UsernameEntry.Text?.Trim() ?? string.Empty,
                PasswordEntry.Text ?? string.Empty,
                preset);
            await settings.SaveAsync();
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception exception)
        {
            ConnectionStatusLabel.Text = $"Unable to save connection settings: {exception.Message}";
        }
    }
}