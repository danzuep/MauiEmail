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

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void OnThemeToggled(object? sender, ToggledEventArgs e)
    {
        if (Application.Current is { } application)
            application.UserAppTheme = e.Value ? AppTheme.Dark : AppTheme.Light;
    }
}