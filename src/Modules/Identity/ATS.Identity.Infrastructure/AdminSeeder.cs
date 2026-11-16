using ATS.Identity.Domain;
using ATS.Identity.Domain.Ports;
using ATS.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ATS.Identity.Infrastructure;

/// <summary>Thong tin Admin dau tien, doc tu <c>Seed:AdminEmail</c> / <c>Seed:AdminPassword</c>.</summary>
public sealed record AdminSeedOptions(string Email, string Password, string FullName);

/// <summary>
/// Tao tai khoan Admin dau tien luc khoi dong neu chua co (idempotent).
/// </summary>
/// <remarks>
/// Chi dang ky khi <c>Database:AutoMigrate=true</c> (luc do Program.cs da migrate xong truoc
/// <c>app.Run()</c>, ma hosted service lai chay trong <c>app.Run()</c> — bang da ton tai).
/// Test va WebApplicationFactory khong bat co nay nen khong bao gio cham Postgres.
/// Loi o day chi ghi log: thieu Admin khong dang de lam sap ca API.
/// </remarks>
internal sealed class AdminSeeder : IHostedService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly AdminSeedOptions _options;
    private readonly ILogger<AdminSeeder> _logger;

    public AdminSeeder(IServiceScopeFactory scopes, AdminSeedOptions options, ILogger<AdminSeeder> logger)
    {
        _scopes = scopes;
        _options = options;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var scope = _scopes.CreateAsyncScope();
            await using (scope.ConfigureAwait(false))
            {
                var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                if (await users.GetByEmailAsync(_options.Email, cancellationToken).ConfigureAwait(false) is not null)
                {
                    return;
                }

                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                var clock = scope.ServiceProvider.GetRequiredService<TimeProvider>();
                var admin = User.Create(_options.Email, hasher.Hash(_options.Password), Role.Admin, _options.FullName, clock.GetUtcNow());

                await users.AddAsync(admin, cancellationToken).ConfigureAwait(false);
                await scope.ServiceProvider.GetRequiredService<IUnitOfWork>()
                    .SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Da tao tai khoan Admin dau tien.");
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Khong tao duoc tai khoan Admin dau tien.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
