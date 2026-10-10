using OutlookClone.Views;

namespace OutlookClone;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(ComposePage), typeof(ComposePage));
		Routing.RegisterRoute(nameof(MessageReaderPage), typeof(MessageReaderPage));
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
	}
}
