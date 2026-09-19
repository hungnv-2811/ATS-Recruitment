namespace ATS.SharedKernel;

/// <summary>
/// Ket qua thanh cong/that bai ma KHONG dung exception cho luong nghiep vu binh thuong.
/// Xem docs/contracts.md muc 7.
/// </summary>
/// <remarks>
/// Chi co MOT nguon su that la <see cref="Error"/>; <see cref="IsSuccess"/> suy ra tu no.
/// Nho vay trang thai mau thuan (thanh cong ma co loi) khong bieu dien duoc, thay vi
/// bieu dien duoc roi phai viet guard de chan.
/// </remarks>
public class Result
{
    protected Result(Error error) => Error = error;

    public Error Error { get; }

    public bool IsSuccess => Error == Error.None;

    public bool IsFailure => !IsSuccess;

    public static Result Success() => new(Error.None);

    public static Result Failure(Error error) => new(MustBeReal(error));

    public static Result<TValue> Success<TValue>(TValue value) => new(value, Error.None);

    public static Result<TValue> Failure<TValue>(Error error) => new(default, MustBeReal(error));

    private static Error MustBeReal(Error error)
        => error == Error.None
            ? throw new ArgumentException("Ket qua that bai phai kem loi.", nameof(error))
            : error;
}

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, Error error)
        : base(error)
        => _value = value;

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Khong doc duoc Value cua mot ket qua that bai.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}
