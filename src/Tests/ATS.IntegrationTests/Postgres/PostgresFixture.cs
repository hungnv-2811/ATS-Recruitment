using ATS.AiScreening.Infrastructure;
using ATS.Identity.Infrastructure;
using ATS.Persistence;
using ATS.Persistence.Reporting;
using ATS.Recruitment.Infrastructure;
using ATS.Recruitment.Infrastructure.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace ATS.IntegrationTests.Postgres;

/// <summary>
/// Database Postgres TAM cho moi lan chay: tao moi, ap migration that, seed demo, xoa khi xong.
/// </summary>
/// <remarks>
/// Chi chay khi co bien moi truong <c>ATS_TEST_POSTGRES</c> = connection string toi mot server
/// Postgres (user co quyen CREATE DATABASE), vd
/// <c>Host=localhost;Port=15433;Username=ats;Password=Dev_Password123!;Database=postgres</c>.
/// CI (.github/workflows/ci.yml) dung service postgres:16 va dat bien nay, nen tren CI bo test
/// LUON chay that. May khong co bien → cac test tra ve som (xem <see cref="IsAvailable"/>).
///
/// Ly do ton tai: InMemory khong chay SQL tho (bao cao, quota ON CONFLICT, seeder) va khong ep
/// unique/FK — dung nhung cho de vo nhat chi lo ra tren database that.
/// </remarks>
public sealed class PostgresFixture : IAsyncLifetime
{
    public const string EnvironmentVariable = "ATS_TEST_POSTGRES";

    private readonly string? _serverConnection = Environment.GetEnvironmentVariable(EnvironmentVariable);
    private readonly string _databaseName = $"ats_test_{Guid.NewGuid():N}";
    private readonly string _fileRoot = Path.Combine(Path.GetTempPath(), $"ats-pg-test-{Guid.NewGuid():N}");
    private ServiceProvider? _services;

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_serverConnection);

    public string ConnectionString { get; private set; } = string.Empty;

    /// <summary>Ket qua lan seed DAU TIEN (true = da ghi du lieu).</summary>
    public bool FirstSeedInserted { get; private set; }

    public IServiceProvider Services => _services ?? throw new InvalidOperationException("Fixture chua khoi tao.");

    public async Task InitializeAsync()
    {
        if (!IsAvailable)
        {
            return;
        }

        await using (var admin = new NpgsqlConnection(_serverConnection))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{_databaseName}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        ConnectionString = new NpgsqlConnectionStringBuilder(_serverConnection) { Database = _databaseName }.ConnectionString;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AiProvider"] = "Fake",
                ["Redis:ConnectionString"] = string.Empty,
                ["FileStorage:RootPath"] = _fileRoot,
                ["Ai:PreviewLimitPerDay"] = "20",
                ["Ai:BatchLimitPerDay"] = "5",
                ["Jwt:Secret"] = "postgres-test-secret-at-least-32-characters!!",
                ["Jwt:Issuer"] = "ats-test",
                ["Jwt:Audience"] = "ats-test-users",
            })
            .Build();

        // Cung wiring voi Composition Root cua ATS.Api (thu tu co y nghia — xem Program.cs).
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddAtsPersistence(
            ConnectionString,
            typeof(ATS.Recruitment.Infrastructure.InfrastructureAssemblyMarker).Assembly,
            typeof(ATS.AiScreening.Infrastructure.InfrastructureAssemblyMarker).Assembly,
            typeof(ATS.Identity.Infrastructure.InfrastructureAssemblyMarker).Assembly);
        services.AddSharedInfrastructure(configuration);
        services.AddIdentityModule(configuration);
        services.AddRecruitmentModule(configuration);
        services.AddAiScreeningModule(configuration);
        services.AddAtsReporting();
        _services = services.BuildServiceProvider();

        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AtsDbContext>();
        await db.Database.MigrateAsync();
        FirstSeedInserted = await DatabaseSeeder.SeedAsync(db, _fileRoot);
    }

    public async Task DisposeAsync()
    {
        if (!IsAvailable)
        {
            return;
        }

        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        NpgsqlConnection.ClearAllPools();
        await using (var admin = new NpgsqlConnection(_serverConnection))
        {
            await admin.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }

        if (Directory.Exists(_fileRoot))
        {
            Directory.Delete(_fileRoot, recursive: true);
        }
    }

    public async Task<long> ScalarAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return Convert.ToInt64(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<IReadOnlyList<object[]>> RowsAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<object[]>();
        while (await reader.ReadAsync())
        {
            var values = new object[reader.FieldCount];
            reader.GetValues(values);
            rows.Add(values);
        }

        return rows;
    }
}
