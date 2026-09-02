namespace TotalMonitor.Core.Entities;

public sealed class User
{
    public int Id { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string PasswordSalt { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public int FailedLoginCount { get; private set; }
    public DateTimeOffset? LockoutUntil { get; private set; }
    private User() { }
    public User(string userName, bool isActive = true) : this(userName, userName, string.Empty, string.Empty, isActive) { }
    public User(string userName, string displayName, string passwordHash, string passwordSalt, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(userName)) throw new ArgumentException("A user name is required.", nameof(userName));
        UserName = userName.Trim(); DisplayName = string.IsNullOrWhiteSpace(displayName) ? UserName : displayName.Trim(); PasswordHash = passwordHash; PasswordSalt = passwordSalt; IsActive = isActive; CreatedAt = UpdatedAt = DateTimeOffset.UtcNow;
    }
    public void MarkLoginSuccess(DateTimeOffset at) { LastLoginAt = at; FailedLoginCount = 0; LockoutUntil = null; UpdatedAt = at; }
    public void MarkLoginFailure(DateTimeOffset at, DateTimeOffset? lockoutUntil = null) { FailedLoginCount++; LockoutUntil = lockoutUntil; UpdatedAt = at; }
    public void SetPassword(string hash, string salt) { PasswordHash = hash; PasswordSalt = salt; UpdatedAt = DateTimeOffset.UtcNow; }
    public void SetActive(bool active) { IsActive = active; UpdatedAt = DateTimeOffset.UtcNow; }
}
