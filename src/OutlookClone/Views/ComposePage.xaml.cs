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
        HtmlEditor.LoadHtml(viewModel.MessageTextBox);
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnMailSent()
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnSendClicked(object? sender, EventArgs e)
    {
        if (BindingContext is not SenderViewModel viewModel)
            return;

        viewModel.MessageTextBox = HtmlEditor.GetHtml();
        await viewModel.SendMailCommand.ExecuteAsync(null);
    }
}