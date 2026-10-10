using Microsoft.Maui.Storage;

namespace OutlookClone.Services;

public sealed record MailConnectionSettings(
    string ImapHost,
    ushort ImapPort,
    string SmtpHost,
    ushort SmtpPort,
    string Username,
    string Password)
{
    private const string ImapHostKey = "mail.imap.host";
    private const string ImapPortKey = "mail.imap.port";
    private const string SmtpHostKey = "mail.smtp.host";
    private const string SmtpPortKey = "mail.smtp.port";
    private const string UsernameKey = "mail.username";
    private const string PasswordKey = "mail.password";

    public static async Task<MailConnectionSettings> LoadAsync()
    {
        var password = await SecureStorage.Default.GetAsync(PasswordKey) ?? string.Empty;
        return new MailConnectionSettings(
            Preferences.Default.Get(ImapHostKey, "localhost"),
            ReadPort(ImapPortKey, 143),
            Preferences.Default.Get(SmtpHostKey, "localhost"),
            ReadPort(SmtpPortKey, 25),
            Preferences.Default.Get(UsernameKey, string.Empty),
            password);
    }

    public async Task SaveAsync()
    {
        Preferences.Default.Set(ImapHostKey, ImapHost);
        Preferences.Default.Set(ImapPortKey, (int)ImapPort);
        Preferences.Default.Set(SmtpHostKey, SmtpHost);
        Preferences.Default.Set(SmtpPortKey, (int)SmtpPort);
        Preferences.Default.Set(UsernameKey, Username);

        if (string.IsNullOrEmpty(Password))
            SecureStorage.Default.Remove(PasswordKey);
        else
            await SecureStorage.Default.SetAsync(PasswordKey, Password);
    }

    private static ushort ReadPort(string key, int defaultPort) =>
        (ushort)Math.Clamp(Preferences.Default.Get(key, defaultPort), 1, ushort.MaxValue);
}