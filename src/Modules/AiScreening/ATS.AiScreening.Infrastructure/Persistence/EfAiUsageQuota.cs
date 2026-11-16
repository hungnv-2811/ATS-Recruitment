using System.Data.Common;
using ATS.AiScreening.Application;
using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Entities;
using ATS.AiScreening.Domain.Ports;
using ATS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ATS.AiScreening.Infrastructure.Persistence;

/// <summary>
/// Han muc AI theo (user, ngay UTC) tren bang <c>aiscreening.ai_usage_quotas</c> — RB2.
/// </summary>
/// <remarks>
/// PostgreSQL: MOT cau <c>INSERT ... ON CONFLICT DO UPDATE ... WHERE count &lt; limit RETURNING</c> —
/// tang dem va kiem tran trong cung mot lenh nguyen tu, hai request dua nhau khong vuot tran duoc.
/// Provider khac (EF InMemory trong test): doc-sua-ghi qua entity <see cref="AiUsageQuota"/>.
/// </remarks>
public sealed class EfAiUsageQuota : IAiUsageQuota
{
    private const string PreviewColumn = "preview_count";
    private const string BatchColumn = "batch_count";

    private readonly AtsDbContext _db;
    private readonly AiScreeningOptions _options;
    private readonly TimeProvider _clock;

    public EfAiUsageQuota(AtsDbContext db, AiScreeningOptions options, TimeProvider clock)
    {
        _db = db;
        _options = options;
        _clock = clock;
    }

    public Task<bool> TryConsumePreviewAsync(Guid userId, CancellationToken ct = default)
        => TryConsumeAsync(userId, PreviewColumn, _options.PreviewLimitPerDay, ct);

    public Task<bool> TryConsumeBatchAsync(Guid userId, CancellationToken ct = default)
        => TryConsumeAsync(userId, BatchColumn, _options.BatchLimitPerDay, ct);

    public async Task<QuotaStatus> GetStatusAsync(Guid userId, CancellationToken ct = default)
    {
        var today = Today();
        var row = await _db.Set<AiUsageQuota>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.UsageDate == today, ct)
            .ConfigureAwait(false);

        return new QuotaStatus(
            row?.PreviewCount ?? 0,
            _options.PreviewLimitPerDay,
            row?.BatchCount ?? 0,
            _options.BatchLimitPerDay);
    }

    private DateOnly Today() => DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);

    private async Task<bool> TryConsumeAsync(Guid userId, string column, int limit, CancellationToken ct)
    {
        if (limit <= 0)
        {
            return false;
        }

        return _db.Database.IsNpgsql()
            ? await TryConsumeAtomicAsync(userId, column, limit, ct).ConfigureAwait(false)
            : await TryConsumeWithEntityAsync(userId, column, limit, ct).ConfigureAwait(false);
    }

    private async Task<bool> TryConsumeAtomicAsync(Guid userId, string column, int limit, CancellationToken ct)
    {
        // `column` chi nhan mot trong hai hang so o tren — khong bao gio la du lieu nguoi dung.
        var isPreview = column == PreviewColumn;
        var sql = $"""
            INSERT INTO {AtsDbContext.AiScreeningSchema}.ai_usage_quotas
                   (id, user_id, usage_date, preview_count, batch_count, updated_at)
            VALUES (@id, @user_id, @usage_date, @preview_init, @batch_init, @now)
            ON CONFLICT (user_id, usage_date) DO UPDATE
               SET {column} = ai_usage_quotas.{column} + 1,
                   updated_at = EXCLUDED.updated_at
             WHERE ai_usage_quotas.{column} < @limit
            RETURNING {column};
            """;

        var connection = _db.Database.GetDbConnection();
        await _db.Database.OpenConnectionAsync(ct).ConfigureAwait(false);
        try
        {
            var command = connection.CreateCommand();
            await using (command.ConfigureAwait(false))
            {
                command.CommandText = sql;
                command.Transaction = _db.Database.CurrentTransaction?.GetDbTransaction();
                AddParameter(command, "id", Guid.CreateVersion7());
                AddParameter(command, "user_id", userId);
                AddParameter(command, "usage_date", Today());
                AddParameter(command, "preview_init", isPreview ? 1 : 0);
                AddParameter(command, "batch_init", isPreview ? 0 : 1);
                AddParameter(command, "now", _clock.GetUtcNow());
                AddParameter(command, "limit", limit);

                // Khong co dong tra ve = da cham tran (nhanh DO UPDATE bi WHERE chan).
                var returned = await command.ExecuteScalarAsync(ct).ConfigureAwait(false);
                return returned is not null and not DBNull;
            }
        }
        finally
        {
            await _db.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }

    private async Task<bool> TryConsumeWithEntityAsync(Guid userId, string column, int limit, CancellationToken ct)
    {
        var today = Today();
        var now = _clock.GetUtcNow();
        var row = await _db.Set<AiUsageQuota>()
            .FirstOrDefaultAsync(x => x.UserId == userId && x.UsageDate == today, ct)
            .ConfigureAwait(false);

        if (row is null)
        {
            row = AiUsageQuota.Create(userId, today, now);
            _db.Add(row);
        }

        var consumed = column == PreviewColumn
            ? row.TryConsumePreview(limit, now)
            : row.TryConsumeBatch(limit, now);

        if (consumed)
        {
            await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        return consumed;
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
