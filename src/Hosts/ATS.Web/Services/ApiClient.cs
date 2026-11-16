using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ATS.Web.Services;

/// <summary>Ket qua mot lan goi API — UI khong bao gio phai bat exception HTTP.</summary>
public class ApiResult
{
    protected ApiResult(int statusCode, string? error, string? errorCode, JsonElement? problem)
    {
        StatusCode = statusCode;
        Error = error;
        ErrorCode = errorCode;
        Problem = problem;
    }

    public int StatusCode { get; }

    /// <summary>Thong bao loi da dich sang tieng Viet de hien thang len man hinh.</summary>
    public string? Error { get; }

    /// <summary>Truong <c>code</c> cua ProblemDetails (contracts.md 8.1), vd "quota_exceeded".</summary>
    public string? ErrorCode { get; }

    /// <summary>Body ProblemDetails tho — doc them truong mo rong (vd previewRemainingToday).</summary>
    public JsonElement? Problem { get; }

    public bool IsSuccess => Error is null;

    public static ApiResult Ok(int status) => new(status, null, null, null);

    public static ApiResult Fail(int status, string error, string? code = null, JsonElement? problem = null)
        => new(status, error, code, problem);
}

public sealed class ApiResult<T> : ApiResult
{
    private ApiResult(int statusCode, T? value, string? error, string? errorCode, JsonElement? problem)
        : base(statusCode, error, errorCode, problem)
        => Value = value;

    public T? Value { get; }

    public static ApiResult<T> Ok(int status, T? value) => new(status, value, null, null, null);

    public static ApiResult<T> From(ApiResult failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new(failure.StatusCode, default, failure.Error, failure.ErrorCode, failure.Problem);
    }
}

/// <summary>File tai ve tu API (CV goc).</summary>
public sealed record DownloadedFile(string FileName, string ContentType, byte[] Content);

/// <summary>
/// Cua DUY NHAT ra API. Gan JWT cua phien hien tai, doi ProblemDetails thanh thong bao doc duoc.
/// </summary>
/// <remarks>
/// Trang .razor khong goi HttpClient truc tiep (code-review.md muc 8) — di qua cac *Api
/// service, cac service do di qua lop nay.
/// </remarks>
public sealed class ApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _factory;
    private readonly AuthSession _session;

    public ApiClient(IHttpClientFactory factory, AuthSession session)
    {
        _factory = factory;
        _session = session;
    }

    public Task<ApiResult<T>> GetAsync<T>(string url, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Get, url, null, ct);

    public Task<ApiResult<T>> PostAsync<T>(string url, object? body, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Post, url, ToJson(body), ct);

    public Task<ApiResult<T>> PatchAsync<T>(string url, object? body, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Patch, url, ToJson(body), ct);

    public Task<ApiResult<T>> PostContentAsync<T>(string url, HttpContent content, CancellationToken ct = default)
        => SendAsync<T>(HttpMethod.Post, url, content, ct);

    public async Task<ApiResult> PostAsync(string url, object? body, CancellationToken ct = default)
        => await SendAsync<JsonElement?>(HttpMethod.Post, url, ToJson(body), ct).ConfigureAwait(false);

    public async Task<ApiResult> PutAsync(string url, object? body, CancellationToken ct = default)
        => await SendAsync<JsonElement?>(HttpMethod.Put, url, ToJson(body), ct).ConfigureAwait(false);

    public async Task<ApiResult> PatchAsync(string url, object? body, CancellationToken ct = default)
        => await SendAsync<JsonElement?>(HttpMethod.Patch, url, ToJson(body), ct).ConfigureAwait(false);

    public async Task<ApiResult> DeleteAsync(string url, CancellationToken ct = default)
        => await SendAsync<JsonElement?>(HttpMethod.Delete, url, null, ct).ConfigureAwait(false);

    public async Task<ApiResult<DownloadedFile>> DownloadAsync(string url, CancellationToken ct = default)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, url, null);
            using var response = await Client.SendAsync(request, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<DownloadedFile>.From(await ToFailureAsync(response, ct).ConfigureAwait(false));
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(ct).ConfigureAwait(false);
            var name = response.Content.Headers.ContentDisposition?.FileNameStar
                       ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                       ?? "cv";
            var type = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            return ApiResult<DownloadedFile>.Ok((int)response.StatusCode, new DownloadedFile(name, type, bytes));
        }
        catch (HttpRequestException)
        {
            return ApiResult<DownloadedFile>.From(Unreachable());
        }
    }

    private HttpClient Client => _factory.CreateClient("ats-api");

    private async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, HttpContent? content, CancellationToken ct)
    {
        try
        {
            using var request = CreateRequest(method, url, content);
            using var response = await Client.SendAsync(request, ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<T>.From(await ToFailureAsync(response, ct).ConfigureAwait(false));
            }

            if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
            {
                return ApiResult<T>.Ok((int)response.StatusCode, default);
            }

            var value = await response.Content.ReadFromJsonAsync<T>(Json, ct).ConfigureAwait(false);
            return ApiResult<T>.Ok((int)response.StatusCode, value);
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.From(Unreachable());
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return ApiResult<T>.From(ApiResult.Fail(0, "API phản hồi quá lâu. Vui lòng thử lại."));
        }
        catch (JsonException)
        {
            return ApiResult<T>.From(ApiResult.Fail(0, "API trả về dữ liệu không đọc được."));
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url, HttpContent? content)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        var token = _session.Current?.AccessToken;
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    private async Task<ApiResult> ToFailureAsync(HttpResponseMessage response, CancellationToken ct)
    {
        var status = (int)response.StatusCode;
        JsonElement? problem = null;
        string? detail = null;
        string? code = null;

        try
        {
            var text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(text) && text.TrimStart().StartsWith('{'))
            {
                using var document = JsonDocument.Parse(text);
                var root = document.RootElement.Clone();
                problem = root;
                code = ReadString(root, "code");
                detail = ReadValidationErrors(root) ?? ReadString(root, "detail") ?? ReadString(root, "title");
            }
        }
        catch (JsonException)
        {
            // Body khong phai JSON — dung thong bao mac dinh theo status ben duoi.
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized && _session.Current is not null)
        {
            // Token het han/bi thu hoi: dang xuat de RoleGuard dua ve trang dang nhap.
            await _session.SignOutAsync().ConfigureAwait(false);
            return ApiResult.Fail(status, "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.", "unauthorized", problem);
        }

        return ApiResult.Fail(status, detail ?? DefaultMessage(response.StatusCode), code, problem);
    }

    private static string? ReadString(JsonElement root, string name)
        => root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? ReadValidationErrors(JsonElement root)
    {
        if (!root.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var messages = errors.EnumerateObject()
            .SelectMany(p => p.Value.ValueKind == JsonValueKind.Array
                ? p.Value.EnumerateArray().Select(e => e.GetString())
                : [p.Value.ToString()])
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .ToArray();

        return messages.Length == 0 ? null : string.Join(" ", messages);
    }

    private static string DefaultMessage(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => "Bạn cần đăng nhập.",
        HttpStatusCode.Forbidden => "Bạn không có quyền thực hiện thao tác này.",
        HttpStatusCode.NotFound => "Không tìm thấy dữ liệu.",
        HttpStatusCode.Conflict => "Dữ liệu bị trùng hoặc đang được sử dụng.",
        HttpStatusCode.TooManyRequests => "Bạn đã dùng hết lượt hôm nay.",
        _ when (int)status >= 500 => "Máy chủ đang gặp sự cố. Vui lòng thử lại sau.",
        _ => "Yêu cầu không hợp lệ.",
    };

    private static ApiResult Unreachable()
        => ApiResult.Fail(0, "Không kết nối được tới API. Kiểm tra dịch vụ api đã chạy chưa.");

    private static JsonContent? ToJson(object? body)
        => body is null ? null : JsonContent.Create(body, body.GetType(), options: Json);
}
