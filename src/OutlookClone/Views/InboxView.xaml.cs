using System.Collections.ObjectModel;
using MailKit.Search;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Services;
using OutlookClone.Extensions;
using OutlookClone.Services;

namespace OutlookClone.Views;

public partial class InboxView : ContentView
{
    private const int PageSize = 10;
    private bool _isLoading;
    private string _activeSearchText = string.Empty;
    private SearchFields _activeSearchFields = SearchFields.Subject;
    private DateTime? _activeDeliveredBefore;
    private DateTime? _activeDeliveredAfter;
    private List<Models.Email>? _demoEmails;

    public InboxView()
    {
        InitializeComponent();
        BindingContext = this;
        FilterPanel.Dismissed += OnFilterPanelDismissed;
    }

    public ObservableCollection<Models.Email> Emails { get; } = new();

    public Task RefreshAsync() => LoadPageAsync(
        reset: true,
        searchText: _activeSearchText,
        searchFields: _activeSearchFields,
        deliveredBefore: _activeDeliveredBefore,
        deliveredAfter: _activeDeliveredAfter);

    private async Task LoadPageAsync(
        bool reset,
        string? searchText = null,
        SearchFields? searchFields = null,
        DateTime? deliveredBefore = null,
        DateTime? deliveredAfter = null)
    {
        if (_isLoading)
            return;

        if (reset)
        {
            _activeSearchText = searchText?.Trim() ?? string.Empty;
            _activeSearchFields = searchFields ?? _activeSearchFields;
            _activeDeliveredBefore = deliveredBefore;
            _activeDeliveredAfter = deliveredAfter;
            Emails.Clear();
        }

        _isLoading = true;
        LoadingIndicator.IsVisible = true;
        LoadingIndicator.IsRunning = true;
        LoadMoreButton.IsEnabled = false;
        try
        {
            var connectionSettings = await MailConnectionSettings.LoadAsync();
            if (connectionSettings.Preset == MailConnectionSettings.DemoPreset)
            {
                _demoEmails ??= LoadDemoEmails();
                var filteredEmails = FilterDemoEmails(_demoEmails).ToList();
                var page = filteredEmails.Skip(Emails.Count).Take(PageSize).ToList();
                foreach (var email in page)
                    Emails.Add(email);
                LoadMoreButton.IsVisible = filteredEmails.Count > Emails.Count;
            }
            else
            {
                using var receiver = string.IsNullOrWhiteSpace(connectionSettings.Username) && string.IsNullOrWhiteSpace(connectionSettings.Password)
                    ? ImapReceiver.Create(connectionSettings.ImapHost, connectionSettings.ImapPort)
                    : ImapReceiver.Create(connectionSettings.ImapHost, connectionSettings.ImapPort, connectionSettings.Username, connectionSettings.Password);
                IMailReader mailReader = receiver.ReadMail;
                var searchQuery = CreateSearchQuery(
                    _activeSearchFields,
                    _activeSearchText,
                    _activeDeliveredBefore,
                    _activeDeliveredAfter);
                if (searchQuery is not null)
                    mailReader = mailReader.Query(searchQuery);

                var messages = await mailReader
                    .Skip(Emails.Count)
                    .Take(PageSize)
                    .GetMimeMessagesAsync();
                var emails = messages.Convert().ToList();
                foreach (var email in emails)
                    Emails.Add(email);

                LoadMoreButton.IsVisible = emails.Count == PageSize;
            }
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

    private void OnFilterClicked(object? sender, EventArgs e)
    {
        FilterPanel.IsVisible = !FilterPanel.IsVisible;
    }

    private void OnFilterPanelDismissed()
    {
        FilterPanel.IsVisible = false;
    }

    private async Task SearchAsync()
    {
        var searchText = SearchEntry.Text?.Trim() ?? string.Empty;
        var searchFields = GetSelectedSearchFields();
        DateTime? deliveredBefore = null;
        DateTime? deliveredAfter = null;

        if (searchFields.HasFlag(SearchFields.DeliveredBefore))
            deliveredBefore = FilterPanel.DeliveredBeforeDate;

        if (searchFields.HasFlag(SearchFields.DeliveredAfter))
            deliveredAfter = FilterPanel.DeliveredAfterDate;

        if (searchText.Length > 0 && (searchFields & SearchFields.Text) == SearchFields.None)
        {
            await Shell.Current.DisplayAlertAsync("Select a text filter", "Choose From, To, Subject, or Message to search the text box.", "OK");
            return;
        }

        FilterPanel.IsVisible = false;
        await LoadPageAsync(
            reset: true,
            searchText: searchText,
            searchFields: searchFields,
            deliveredBefore: deliveredBefore,
            deliveredAfter: deliveredAfter);
    }

    private SearchFields GetSelectedSearchFields()
    {
        var searchFields = SearchFields.None;
        if (FilterPanel.IsFromSelected) searchFields |= SearchFields.From;
        if (FilterPanel.IsToSelected) searchFields |= SearchFields.To;
        if (FilterPanel.IsSubjectSelected) searchFields |= SearchFields.Subject;
        if (FilterPanel.IsMessageSelected) searchFields |= SearchFields.Message;
        if (FilterPanel.IsDeliveredBeforeSelected) searchFields |= SearchFields.DeliveredBefore;
        if (FilterPanel.IsDeliveredAfterSelected) searchFields |= SearchFields.DeliveredAfter;
        return searchFields;
    }

    private static SearchQuery? CreateSearchQuery(
        SearchFields searchFields,
        string searchText,
        DateTime? deliveredBefore,
        DateTime? deliveredAfter)
    {
        var queries = new List<SearchQuery>();
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            if (searchFields.HasFlag(SearchFields.From))
                queries.Add(SearchQuery.FromContains(searchText));
            if (searchFields.HasFlag(SearchFields.To))
                queries.Add(SearchQuery.ToContains(searchText));
            if (searchFields.HasFlag(SearchFields.Subject))
                queries.Add(SearchQuery.SubjectContains(searchText));
            if (searchFields.HasFlag(SearchFields.Message))
                queries.Add(SearchQuery.MessageContains(searchText));
        }

        if (deliveredBefore.HasValue)
            queries.Add(SearchQuery.DeliveredBefore(deliveredBefore.Value));
        if (deliveredAfter.HasValue)
            queries.Add(SearchQuery.DeliveredAfter(deliveredAfter.Value));

        if (queries.Count == 0)
            return null;

        var query = queries[0];
        foreach (var condition in queries.Skip(1))
            query = query.Or(condition);
        return query;
    }

    private static List<Models.Email> LoadDemoEmails()
    {
        string[] senders = [
            "Alex Morgan <alex@example.test>",
            "Jamie Chen <jamie@example.test>",
            "Riley Patel <riley@example.test>",
            "Taylor Brooks <taylor@example.test>",
            "Casey Rivera <casey@example.test>",
            "Jordan Kim <jordan@example.test>"];
        string[] subjects = [
            "Project update", "Invoice 1042", "Meeting notes", "Travel itinerary",
            "Design review", "Weekly summary", "Order confirmation", "Account notice"];
        string[] bodies = [
            "Here is the latest update. The team finished the first milestone and is reviewing the remaining tasks.",
            "Please review the attached invoice details and let us know if anything needs to be corrected.",
            "Notes from today's meeting: confirm owners, update the schedule, and share the revised plan.",
            "Your itinerary is ready. Departure is in the morning, and the hotel details are included below.",
            "The design review is scheduled for next week. Please add comments before the meeting.",
            "This week's summary includes completed work, open questions, and priorities for the next sprint.",
            "Your order has been confirmed and is being prepared. Tracking information will follow shortly.",
            "There is an update to your account profile. Please review the details when convenient."];
        string[] recipients = [
            "you@example.test", "team@example.test", "sam@example.test", "support@example.test"];
        var random = new Random(0x51A7);
        var now = DateTimeOffset.Now;

        return Enumerable.Range(0, 40)
            .Select(index =>
            {
                var bodyIndex = random.Next(bodies.Length);
                var received = now.AddDays(-random.Next(0, 46))
                    .AddHours(-random.Next(0, 24))
                    .AddMinutes(-random.Next(0, 60));
                return new Models.Email
                {
                    From = senders[random.Next(senders.Length)],
                    To = recipients[random.Next(recipients.Length)],
                    Cc = index % 4 == 0 ? "project@example.test" : string.Empty,
                    Bcc = index % 7 == 0 ? "archive@example.test" : string.Empty,
                    Subject = subjects[(bodyIndex + index) % subjects.Length],
                    Date = received.ToString("R"),
                    BodyText = bodies[bodyIndex],
                    BodyHtml = bodies[bodyIndex]
                };
            })
            .ToList();
    }

    private IEnumerable<Models.Email> FilterDemoEmails(IEnumerable<Models.Email> emails)
    {
        var hasText = !string.IsNullOrWhiteSpace(_activeSearchText);
        var hasDateFilters = _activeDeliveredBefore.HasValue || _activeDeliveredAfter.HasValue;
        if (!hasText && !hasDateFilters)
            return emails;

        return emails.Where(email =>
        {
            var textMatch = hasText &&
                ((_activeSearchFields.HasFlag(SearchFields.From) &&
                  (email.From.Contains(_activeSearchText, StringComparison.OrdinalIgnoreCase) ||
                   email.Cc.Contains(_activeSearchText, StringComparison.OrdinalIgnoreCase) ||
                   email.Bcc.Contains(_activeSearchText, StringComparison.OrdinalIgnoreCase))) ||
                 (_activeSearchFields.HasFlag(SearchFields.To) && email.To.Contains(_activeSearchText, StringComparison.OrdinalIgnoreCase)) ||
                 (_activeSearchFields.HasFlag(SearchFields.Subject) && email.Subject.Contains(_activeSearchText, StringComparison.OrdinalIgnoreCase)) ||
                 (_activeSearchFields.HasFlag(SearchFields.Message) && email.BodyText.Contains(_activeSearchText, StringComparison.OrdinalIgnoreCase)));
            var received = DateTimeOffset.TryParse(email.Date, out var parsedDate) ? parsedDate.Date : DateTimeOffset.MinValue;
            var beforeMatch = _activeDeliveredBefore.HasValue && received < _activeDeliveredBefore.Value.Date;
            var afterMatch = _activeDeliveredAfter.HasValue && received >= _activeDeliveredAfter.Value.Date;
            return textMatch || beforeMatch || afterMatch;
        });
    }

    [Flags]
    private enum SearchFields
    {
        None = 0,
        From = 1,
        To = 2,
        Subject = 4,
        Message = 8,
        DeliveredBefore = 16,
        DeliveredAfter = 32,
        Text = From | To | Subject | Message,
        Dates = DeliveredBefore | DeliveredAfter
    }

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

    private async void OnSettingsClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }
}