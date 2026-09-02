using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TotalMonitor.Infrastructure.Persistence;
using TotalMonitor.Infrastructure.Security;
using TotalMonitor.Core.Security;

namespace TotalMonitor.Core.Tests;

public sealed class IdentitySeederTests
{
    [Fact]
    public async Task Creates_initial_administrator_when_none_exists()
    {
        await using var db = CreateDb();
        var logger = new TestLogger<IdentitySeeder>();
        var seeder = new IdentitySeeder(db, new PasswordHasher(), logger);

        await seeder.EnsureInitialAdministratorAsync("admin", "Administrador", "UnaClaveSegura1");

        var user = await db.Users.SingleAsync();
        Assert.Equal("admin", user.UserName);
        Assert.True(new PasswordHasher().Verify("UnaClaveSegura1", user.PasswordHash, user.PasswordSalt));
        Assert.Equal(RoleNames.Administrator, await db.UserRoles.Join(db.Roles, ur => ur.RoleId, r => r.Id, (_, r) => r.Name).SingleAsync());
    }

    [Fact]
    public async Task Does_not_duplicate_existing_administrator_or_change_password()
    {
        await using var db = CreateDb();
        var seeder = new IdentitySeeder(db, new PasswordHasher(), new TestLogger<IdentitySeeder>());

        await seeder.EnsureInitialAdministratorAsync("admin", "Administrador", "UnaClaveSegura1");
        var originalHash = (await db.Users.SingleAsync()).PasswordHash;
        await seeder.EnsureInitialAdministratorAsync("admin", "Otro nombre", "OtraClaveSegura2");

        Assert.Equal(1, await db.Users.CountAsync());
        Assert.Equal(originalHash, (await db.Users.SingleAsync()).PasswordHash);
    }

    [Fact]
    public async Task Does_not_create_user_when_initial_credentials_are_empty()
    {
        await using var db = CreateDb();
        var seeder = new IdentitySeeder(db, new PasswordHasher(), new TestLogger<IdentitySeeder>());

        await seeder.EnsureInitialAdministratorAsync("", "Administrador", "");

        Assert.Empty(await db.Users.ToListAsync());
    }

    [Fact]
    public async Task Logs_never_contain_the_initial_password()
    {
        await using var db = CreateDb();
        var logger = new TestLogger<IdentitySeeder>();
        const string password = "UnaClaveSegura1";
        var seeder = new IdentitySeeder(db, new PasswordHasher(), logger);

        await seeder.EnsureInitialAdministratorAsync("", "Administrador", password);

        Assert.DoesNotContain(logger.Messages, message => message.Contains(password, StringComparison.Ordinal));
    }

    private static TotalMonitorDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TotalMonitorDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TotalMonitorDbContext(options);
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();
            public void Dispose() { }
        }
    }
}
