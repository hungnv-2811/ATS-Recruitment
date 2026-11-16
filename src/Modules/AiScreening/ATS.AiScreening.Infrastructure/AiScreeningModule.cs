using System.ClientModel;
using System.ClientModel.Primitives;
using ATS.AiScreening.Application;
using ATS.AiScreening.Application.Abstractions;
using ATS.AiScreening.Application.Handlers;
using ATS.AiScreening.Application.Ports;
using ATS.AiScreening.Application.Scoring;
using ATS.AiScreening.Domain.Ports;
using ATS.AiScreening.Domain.Services;
using ATS.AiScreening.Infrastructure.CrossModule;
using ATS.AiScreening.Infrastructure.Persistence;
using ATS.AiScreening.Infrastructure.Queue;
using ATS.AiScreening.Infrastructure.Scoring;
using ATS.Recruitment.Application.Contracts;
using ATS.Recruitment.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace ATS.AiScreening.Infrastructure;

/// <summary>
/// Dang ky module AiScreening (port AI, pipeline fallback, queue, cache, quota). — TV4 so huu.
/// </summary>
/// <remarks>
/// Goi SAU AddRecruitmentModule: adapter cross-module o day (ScreeningTriggerAdapter,
/// score reader) ghi de ban NoOp ma Recruitment dang ky bang TryAdd.
/// Ca API lan Worker deu goi ham nay voi CUNG cau hinh AiProvider.
///
/// Cau hinh doc:
/// <list type="bullet">
/// <item><c>AiProvider</c>: "Fake" (mac dinh) = chi FakeAiScoringAdapter + cau hoi mau; khac = pipeline that.</item>
/// <item><c>OpenAI:ApiKey</c> (hoac bien moi truong <c>OPENAI_API_KEY</c>): khong co thi bo qua 2 tang OpenAI, chi con Keyword.</item>
/// <item><c>OpenAI:Model</c> (gpt-4o-mini), <c>OpenAI:EmbeddingModel</c> (text-embedding-3-small).</item>
/// <item><c>Ai:PreviewLimitPerDay</c> (20), <c>Ai:BatchLimitPerDay</c> (5), <c>Ai:PreviewTimeoutSeconds</c> (30),
/// <c>Ai:MaxApplicationsPerBatch</c> (500), <c>Ai:QuestionTimeoutSeconds</c> (45).</item>
/// <item><c>Redis:ConnectionString</c>: trong = hang doi trong bo nho (chi mot process).</item>
/// </list>
/// Khong dang ky gi mo socket luc khoi dong: Redis va OpenAI deu ket noi LUOI o lan goi dau.
/// </remarks>
public static class AiScreeningModule
{
    public const string FakeProvider = "Fake";

    public static IServiceCollection AddAiScreeningModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var provider = configuration["AiProvider"] ?? FakeProvider;
        var isFake = string.Equals(provider, FakeProvider, StringComparison.OrdinalIgnoreCase);
        var apiKey = configuration["OpenAI:ApiKey"] ?? configuration["OPENAI_API_KEY"];
        var useOpenAi = !isFake && !string.IsNullOrWhiteSpace(apiKey);
        var chatModel = configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        var embeddingModel = configuration["OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";

        var options = new AiScreeningOptions
        {
            Provider = provider,
            ModelVersion = isFake ? "fake-v1" : chatModel,
            PromptVersion = Prompts.ScoringVersion,
            PreviewLimitPerDay = configuration.GetValue("Ai:PreviewLimitPerDay", 20),
            BatchLimitPerDay = configuration.GetValue("Ai:BatchLimitPerDay", 5),
            PreviewTimeoutSeconds = Math.Max(1, configuration.GetValue("Ai:PreviewTimeoutSeconds", 30)),
            MaxApplicationsPerBatch = configuration.GetValue("Ai:MaxApplicationsPerBatch", 500),
        };
        var questionBudget = TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue("Ai:QuestionTimeoutSeconds", 45)));

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton(options);

        // --- Domain: an danh (IPiiRedactor tuy chon, phase 2) ---
        services.AddSingleton<IAnonymizer>(sp => new SimpleAnonymizer(sp.GetService<IPiiRedactor>()));

        // --- Cham diem + sinh cau hoi ---
        services.AddSingleton<KeywordScoringAdapter>();
        services.AddSingleton<FakeAiScoringAdapter>();
        services.AddSingleton<TemplateInterviewQuestionAdapter>();

        if (isFake)
        {
            services.AddSingleton<IAiScoringService>(sp => sp.GetRequiredService<FakeAiScoringAdapter>());
            services.AddSingleton<IInterviewQuestionGenerator>(sp => sp.GetRequiredService<TemplateInterviewQuestionAdapter>());
        }
        else
        {
            if (useOpenAi)
            {
                var clientOptions = new OpenAIClientOptions
                {
                    NetworkTimeout = TimeSpan.FromSeconds(options.PreviewTimeoutSeconds),
                    RetryPolicy = new ClientRetryPolicy(maxRetries: 3),
                };
                var credential = new ApiKeyCredential(apiKey!);
                services.AddSingleton(new ChatClient(chatModel, credential, clientOptions));
                services.AddSingleton(new EmbeddingClient(embeddingModel, credential, clientOptions));
                services.AddSingleton<OpenAiScoringAdapter>();
                services.AddSingleton<EmbeddingScoringAdapter>();
                services.AddSingleton<OpenAiInterviewQuestionAdapter>();
            }

            services.AddSingleton<IAiScoringService>(sp =>
            {
                IReadOnlyList<IAiScoringService> aiTiers = useOpenAi
                    ? [sp.GetRequiredService<OpenAiScoringAdapter>(), sp.GetRequiredService<EmbeddingScoringAdapter>()]
                    : [];
                return new AiScoringPipeline(
                    aiTiers,
                    sp.GetRequiredService<KeywordScoringAdapter>(),
                    TimeSpan.FromSeconds(options.PreviewTimeoutSeconds),
                    sp.GetRequiredService<ILogger<AiScoringPipeline>>());
            });

            services.AddSingleton<IInterviewQuestionGenerator>(sp => new InterviewQuestionPipeline(
                useOpenAi ? sp.GetRequiredService<OpenAiInterviewQuestionAdapter>() : null,
                sp.GetRequiredService<TemplateInterviewQuestionAdapter>(),
                questionBudget,
                sp.GetRequiredService<ILogger<InterviewQuestionPipeline>>()));
        }

        // --- Hang doi ---
        var redis = configuration["Redis:ConnectionString"];
        if (string.IsNullOrWhiteSpace(redis))
        {
            services.AddSingleton<IScreeningQueue, InMemoryScreeningQueue>();
        }
        else
        {
            services.AddSingleton<IScreeningQueue>(_ => new RedisScreeningQueue(redis));
        }

        // --- Persistence (AtsDbContext do Composition Root dang ky qua AddAtsPersistence) ---
        services.AddScoped<IAiScoreRepository, EfAiScoreRepository>();
        services.AddScoped<IScreeningJobRepository, EfScreeningJobRepository>();
        services.AddScoped<IInterviewQuestionRepository, EfInterviewQuestionRepository>();
        services.AddScoped<IAiScoreCacheRepository, EfAiScoreCacheRepository>();
        services.AddScoped<IAiUsageQuota, EfAiUsageQuota>();

        // --- Application ---
        services.AddScoped<CachedScoringService>();
        services.AddScoped<PreviewScoreHandler>();
        services.AddScoped<StartScreeningHandler>();
        services.AddScoped<IProcessScreeningHandler, ProcessScreeningHandler>();
        services.AddScoped<GenerateQuestionsHandler>();
        services.AddScoped<InterviewQuestionsHandler>();
        services.AddScoped<AiScreeningQueries>();

        // --- Cross-module ---
        // Doc du lieu tuyen dung qua contract cua Recruitment. Ban rong chi la luoi an toan khi
        // Recruitment chua dang ky ban that (TryAdd: khong bao gio de len ban cua TV2).
        services.TryAddScoped<IRecruitmentQueries, EmptyRecruitmentQueries>();
        services.AddScoped<IRecruitmentReader, RecruitmentReaderAdapter>();

        // AddScoped thuong (KHONG TryAdd): dang ky sau cung nen ghi de NoOp cua Recruitment.
        services.AddScoped<IApplicationScreeningTrigger, ScreeningTriggerAdapter>();
        services.AddScoped<IApplicationScoreReader, ApplicationScoreReader>();

        return services;
    }
}
