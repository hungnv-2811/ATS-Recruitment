namespace ATS.SharedKernel;

/// <summary>
/// Ket qua thanh cong/that bai ma KHONG dung exception cho luong nghiep vu binh thuong.
/// Xem docs/contracts.md muc 7.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Ket qua thanh cong khong duoc kem loi.");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Ket qua that bai phai kem loi.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static Result Success() => new(true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
        => _value = value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Khong doc duoc Value cua mot ket qua that bai.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
