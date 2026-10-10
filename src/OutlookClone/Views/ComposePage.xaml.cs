using OutlookClone.ViewModels;
using RichEdit.Maui;
using RtfPipe;

namespace OutlookClone.Views;

public partial class ComposePage : ContentPage
{
    public ComposePage()
    {
        InitializeComponent();
        var viewModel = new SenderViewModel();
        viewModel.MailSent += OnMailSent;
        BindingContext = viewModel;
        HtmlEditor.Document = RichTextDocument.FromPlainText(viewModel.MessageTextBox);
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

        viewModel.MessageTextBox = Rtf.ToHtml(HtmlEditor.Document.RtfText);
        await viewModel.SendMailCommand.ExecuteAsync(null);
    }
}