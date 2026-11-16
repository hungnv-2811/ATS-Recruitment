using System.Text.Json;
using System.Threading.Channels;
using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Ports;
using StackExchange.Redis;

namespace ATS.AiScreening.Infrastructure.Queue;

/// <summary>
/// Hang doi sang loc tren Redis LIST (ADR-2): API <c>LPUSH</c>, Worker <c>RPOP</c> → FIFO.
/// </summary>
/// <remarks>
/// Ket noi LUOI: khong mo socket luc khoi dong (DI ValidateOnBuild trong test, API van boot khi Redis
/// chua len). <c>AbortOnConnectFail=false</c>: Redis chet roi song lai thi multiplexer tu noi lai.
/// Dung RPOP chu khong BRPOP: StackExchange.Redis da kenh tren mot ket noi nen khong ho tro lenh
/// chan; Worker tu nghi giua cac lan hang doi rong.
/// </remarks>
public sealed class RedisScreeningQueue : IScreeningQueue, IAsyncDisposable, IDisposable
{
    public const string QueueKey = "ats:aiscreening:queue";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ConfigurationOptions _options;
    private readonly SemaphoreSlim _connectGate = new(1, 1);
    private ConnectionMultiplexer? _connection;

    public RedisScreeningQueue(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _options = ConfigurationOptions.Parse(connectionString);
        _options.AbortOnConnectFail = false;
    }

    public async Task EnqueueAsync(ScreeningTask task, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
        await db.ListLeftPushAsync(QueueKey, JsonSerializer.Serialize(task, Json)).ConfigureAwait(false);
    }

    public async Task<ScreeningTask?> DequeueAsync(CancellationToken ct = default)
    {
        var db = await GetDatabaseAsync(ct).ConfigureAwait(false);
        var value = await db.ListRightPopAsync(QueueKey).ConfigureAwait(false);
        return value.IsNullOrEmpty ? null : JsonSerializer.Deserialize<ScreeningTask>(value.ToString(), Json);
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
        }

        _connectGate.Dispose();
    }

    public void Dispose()
    {
        _connection?.Dispose();
        _connectGate.Dispose();
    }

    private async Task<IDatabase> GetDatabaseAsync(CancellationToken ct)
    {
        if (_connection is null)
        {
            await _connectGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                _connection ??= await ConnectionMultiplexer.ConnectAsync(_options).ConfigureAwait(false);
            }
            finally
            {
                _connectGate.Release();
            }
        }

        return _connection.GetDatabase();
    }
}

/// <summary>
/// Hang doi trong bo nho — dung khi <c>Redis:ConnectionString</c> trong (dev mot process, test).
/// </summary>
/// <remarks>
/// CHI song trong mot process: API enqueue vao day thi Worker (process khac) KHONG thay.
/// Muon chay du luong API → Worker thi phai cau hinh Redis (docker compose da co san).
/// </remarks>
public sealed class InMemoryScreeningQueue : IScreeningQueue
{
    private readonly Channel<ScreeningTask> _channel = Channel.CreateUnbounded<ScreeningTask>();

    public int Count => _channel.Reader.Count;

    public Task EnqueueAsync(ScreeningTask task, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(task);
        return _channel.Writer.WriteAsync(task, ct).AsTask();
    }

    public Task<ScreeningTask?> DequeueAsync(CancellationToken ct = default)
        => Task.FromResult(_channel.Reader.TryRead(out var task) ? task : null);
}
