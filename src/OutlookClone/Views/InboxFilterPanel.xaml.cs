namespace OutlookClone.Views;

public partial class InboxFilterPanel : ContentView
{
    public InboxFilterPanel()
    {
        InitializeComponent();
    }

    public event Action? Dismissed;

    public bool IsFromSelected => FromFilter.IsChecked;
    public bool IsToSelected => ToFilter.IsChecked;
    public bool IsSubjectSelected => SubjectFilter.IsChecked;
    public bool IsMessageSelected => MessageFilter.IsChecked;
    public bool IsDeliveredBeforeSelected => DeliveredBeforeFilter.IsChecked;
    public bool IsDeliveredAfterSelected => DeliveredAfterFilter.IsChecked;
    public DateTime? DeliveredBeforeDate => DeliveredBeforePicker.Date;
    public DateTime? DeliveredAfterDate => DeliveredAfterPicker.Date;

    private void OnDoneClicked(object? sender, EventArgs e)
    {
        Dismissed?.Invoke();
    }
}