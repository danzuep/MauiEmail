using OutlookClone.ViewModels;

namespace OutlookClone.Views;

public partial class ComposePage : ContentPage
{
    public ComposePage()
    {
        InitializeComponent();
        var viewModel = new SenderViewModel();
        viewModel.MailSent += OnMailSent;
        BindingContext = viewModel;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnMailSent()
    {
        await Shell.Current.GoToAsync("..");
    }
}