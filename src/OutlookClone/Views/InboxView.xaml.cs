using System.Collections.ObjectModel;
using MailKit.Search;
using MailKitSimplified.Receiver.Abstractions;
using MailKitSimplified.Receiver.Services;
using OutlookClone.Extensions;

namespace OutlookClone.Views;

public partial class InboxView : ContentView
{
    private const int PageSize = 10;
    private bool _isLoading;
    private string _activeSearchText = string.Empty;
    private SearchFields _activeSearchFields = SearchFields.Subject;
    private DateTime? _activeDeliveredBefore;
    private DateTime? _activeDeliveredAfter;

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
            using var receiver = ImapReceiver.Create("localhost");
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
        Text = From | To | Subject | Message
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