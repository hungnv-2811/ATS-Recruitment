using ATS.SharedKernel;

namespace ATS.Api.Http;

/// <summary>
/// Anh xa <see cref="Error"/> sang HTTP status — MOT cho duy nhat cho ca API.
/// </summary>
/// <remarks>
/// Body loi luon la ProblemDetails voi them truong <c>code</c> (vd "validation") de UI
/// hien thong bao ma khong phai doan tu status.
/// </remarks>
public static class ResultHttpExtensions
{
    public static IResult ToProblem(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var status = error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "validation" => StatusCodes.Status400BadRequest,
            "conflict" => StatusCodes.Status409Conflict,
            "forbidden" => StatusCodes.Status403Forbidden,
            "unauthorized" => StatusCodes.Status401Unauthorized,
            "quota_exceeded" => StatusCodes.Status429TooManyRequests,
            "external_service" => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status400BadRequest,
        };

        return Results.Problem(
            detail: error.Message,
            statusCode: status,
            extensions: new Dictionary<string, object?>(StringComparer.Ordinal) { ["code"] = error.Code });
    }

    /// <summary>Thanh cong → <paramref name="onSuccess"/>; that bai → ProblemDetails.</summary>
    public static IResult Match<T>(this Result<T> result, Func<T, IResult> onSuccess)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        return result.IsSuccess ? onSuccess(result.Value) : result.Error.ToProblem();
    }

    /// <summary>Thanh cong → 204; that bai → ProblemDetails.</summary>
    public static IResult ToNoContent(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.IsSuccess ? Results.NoContent() : result.Error.ToProblem();
    }
}
