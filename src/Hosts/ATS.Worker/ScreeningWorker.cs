using ATS.AiScreening.Application.Abstractions;
using ATS.AiScreening.Domain.Ports;
using Microsoft.Extensions.Options;

namespace ATS.Worker;

/// <summary>Cau hinh muc <c>Worker</c>.</summary>
public sealed class ScreeningWorkerOptions
{
    public const string SectionName = "Worker";

    /// <summary>Cho bao lau khi hang doi rong / chua cau hinh / vua loi, truoc khi hoi lai.</summary>
    public TimeSpan IdleDelay { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Doc hang doi Redis va goi pipeline cham diem — phan NANG cua he thong (ADR-2).
/// </summary>
/// <remarks>
/// <para>
/// Tach thanh process rieng de mot lo sang loc 300 CV khong ngon CPU/RAM cua API.
/// Worker chet thi API van phuc vu CRUD binh thuong.
/// </para>
/// <para>
/// Moi viec chay trong MOT scope DI rieng (AtsDbContext la scoped — dung chung giua cac viec
/// thi change tracker phinh ra va loi cua viec truoc lam ban viec sau).
/// </para>
/// <para>
/// <see cref="IScreeningQueue"/> va <see cref="IProcessScreeningHandler"/> duoc resolve LUOI
/// trong vong lap (GetService, khong phai constructor): khi module AiScreening chua dang ky
/// (nhanh feature chua merge) worker van khoi dong, chi ghi log "chua cau hinh" va cho.
/// </para>
/// <para>
/// Loi cua mot viec KHONG giet worker: ghi log roi lam viec tiep theo. Danh dau Failed
/// cho don ung tuyen la viec cua handler (TV4), khong phai cua vong lap nay.
/// </para>
/// </remarks>
public sealed class ScreeningWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScreeningWorker> _logger;
    private readonly TimeSpan _idleDelay;
    private bool _warnedNotConfigured;

    public ScreeningWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<ScreeningWorkerOptions> options,
        ILogger<ScreeningWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _scopeFactory = scopeFactory;
        _logger = logger;
        _idleDelay = options.Value.IdleDelay > TimeSpan.Zero
            ? options.Value.IdleDelay
            : TimeSpan.FromSeconds(5);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScreeningWorker khoi dong (IdleDelay = {IdleDelay}).", _idleDelay);

        while (!stoppingToken.IsCancellationRequested)
        {
            bool processed;
            try
            {
                processed = await ProcessNextAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                // Vi du Redis/Postgres tam mat ket noi: ghi log, nghi mot nhip roi thu lai.
                _logger.LogError(ex, "Loi khi lay/xu ly viec sang loc. Worker tiep tuc chay.");
                processed = false;
            }

            if (processed)
            {
                continue; // con viec thi lam ngay, khong cho
            }

            try
            {
                await Task.Delay(_idleDelay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("ScreeningWorker dung.");
    }

    /// <returns><c>true</c> neu da lay duoc (va xu ly xong hoac loi) mot viec; <c>false</c> neu khong co viec.</returns>
    private async Task<bool> ProcessNextAsync(CancellationToken ct)
    {
        var scope = _scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var queue = scope.ServiceProvider.GetService<IScreeningQueue>();
            var handler = scope.ServiceProvider.GetService<IProcessScreeningHandler>();
            if (queue is null || handler is null)
            {
                // Chi canh bao lan dau de khong spam log moi 5 giay.
                if (!_warnedNotConfigured)
                {
                    _logger.LogWarning(
                        "Module AiScreening chua cau hinh (thieu {Missing}) — worker cho, khong lay viec.",
                        queue is null ? nameof(IScreeningQueue) : nameof(IProcessScreeningHandler));
                    _warnedNotConfigured = true;
                }

                return false;
            }

            _warnedNotConfigured = false;

            var task = await queue.DequeueAsync(ct).ConfigureAwait(false);
            if (task is null)
            {
                return false;
            }

            try
            {
                await handler.HandleAsync(task, ct).ConfigureAwait(false);
                _logger.LogInformation(
                    "Da xu ly viec sang loc: application {ApplicationId}, lo {ScreeningJobId}.",
                    task.ApplicationId, task.ScreeningJobId);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Xu ly viec sang loc that bai: application {ApplicationId}, lo {ScreeningJobId}. Bo qua, lam viec tiep.",
                    task.ApplicationId, task.ScreeningJobId);
            }

            return true;
        }
    }
}
