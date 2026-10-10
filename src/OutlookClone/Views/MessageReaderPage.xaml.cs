namespace OutlookClone.Views;

public partial class MessageReaderPage : ContentPage, IQueryAttributable
{
    public MessageReaderPage()
    {
        InitializeComponent();
    }

    public Models.Email? Email { get; private set; }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue(nameof(Email), out var value) && value is Models.Email email)
        {
            Email = email;
            BindingContext = email;
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}