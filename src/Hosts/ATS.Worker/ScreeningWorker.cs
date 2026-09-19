namespace ATS.Worker;

/// <summary>
/// Doc hang doi Redis va goi LLM cham diem — phan NANG cua he thong (ADR-2).
/// </summary>
/// <remarks>
/// Tach thanh process rieng de mot lo sang loc 300 CV khong ngon CPU/RAM cua API.
/// Worker chet thi API van phuc vu CRUD binh thuong.
///
/// TUAN 2: moi la khung. Tuan 6 moi noi that vao IScreeningQueue + ProcessScreeningHandler.
/// </remarks>
public sealed class ScreeningWorker : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(5);

    private readonly ILogger<ScreeningWorker> _logger;
    private readonly IConfiguration _configuration;

    public ScreeningWorker(ILogger<ScreeningWorker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var aiProvider = _configuration["AiProvider"] ?? "Fake";
        _logger.LogInformation(
            "ScreeningWorker khoi dong. AiProvider = {AiProvider}. Gia tri nay PHAI trung voi API.",
            aiProvider);

        while (!stoppingToken.IsCancellationRequested)
        {
            // TODO tuan 6: DequeueAsync -> ProcessScreeningHandler -> ghi ai_scores + ai_score_cache.
            await Task.Delay(IdleDelay, stoppingToken).ConfigureAwait(false);
        }
    }
}
