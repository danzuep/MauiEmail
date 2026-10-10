using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using MailKitSimplified.Sender.Services;
using OutlookClone.Services;

namespace OutlookClone.ViewModels
{
    public sealed partial class SenderViewModel : ObservableObject
    {
        public event Action? MailSent;

        [ObservableProperty]
        public partial string FromTextBox { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string ToTextBox { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string SubjectTextBox { get; set; } = string.Empty;

        [ObservableProperty]
        public partial string MessageTextBox { get; set; } = string.Empty;

        [ObservableProperty]
        public partial bool IsInProgress { get; set; }

        [ObservableProperty]
        public partial string StatusMessage { get; set; } = string.Empty;

        private int _count = 0;

        private readonly ILogger<SenderViewModel> _logger;

        public SenderViewModel(ILogger<SenderViewModel>? logger = null)
        {
            _logger = logger ?? NullLogger<SenderViewModel>.Instance;
#if DEBUG
            FromTextBox = "from@localhost";
            ToTextBox = "to@localhost";
            SubjectTextBox = "Hey";
            MessageTextBox = "<p>Hi.<p>";
#endif
        }

        [RelayCommand]
        private async Task SendMailAsync()
        {
            IsInProgress = true;
            StatusMessage = string.Empty;
            try
            {
                var connectionSettings = await MailConnectionSettings.LoadAsync();
                if (connectionSettings.Preset == MailConnectionSettings.DemoPreset)
                {
                    StatusMessage = "The local Demo inbox is read-only and cannot send email.";
                    return;
                }
                using var smtpSender = string.IsNullOrWhiteSpace(connectionSettings.Username) && string.IsNullOrWhiteSpace(connectionSettings.Password)
                    ? SmtpSender.Create(connectionSettings.SmtpHost, connectionSettings.SmtpPort)
                    : SmtpSender.Create(connectionSettings.SmtpHost, connectionSettings.SmtpPort, connectionSettings.Username, connectionSettings.Password);
                await smtpSender.WriteEmail
                    .From(FromTextBox)
                    .To(ToTextBox)
                    .Subject(SubjectTextBox)
                    .BodyHtml(MessageTextBox)
                    .SendAsync();
                StatusMessage = $"Email #{++_count} sent.";
                MailSent?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                StatusMessage = $"Send failed: {ex.Message}";
            }
            finally
            {
                IsInProgress = false;
            }
        }
    }
}