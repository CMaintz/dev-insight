namespace DevInsight.Domain.Users;

/// <summary>Decides which commits belong to a user for USER_CONTRIBUTION analysis.</summary>
public sealed class ContributorIdentity
{
    private readonly HashSet<string> _emails;

    public ContributorIdentity(string login, IEnumerable<string> emails)
    {
        Login = login;
        _emails = new HashSet<string>(emails, StringComparer.OrdinalIgnoreCase);
    }

    public string Login { get; }

    public bool Authored(string authorEmail) => _emails.Contains(authorEmail);
}
