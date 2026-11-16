namespace ATS.SharedKernel;

/// <summary>
/// Ghi vao database vi pham rang buoc UNIQUE — thuong la hai request dong thoi cung vuot qua
/// buoc kiem "da ton tai chua" roi cung INSERT.
/// </summary>
/// <remarks>
/// Kieu trung lap, chi dung BCL: tang Application bat duoc ma khong phai biet Npgsql/EF
/// (quy tac 1). <c>AtsDbContext</c> dich loi Postgres 23505 sang kieu nay.
/// </remarks>
public sealed class DuplicateKeyException : Exception
{
    public DuplicateKeyException()
        : base("Vi pham rang buoc duy nhat.")
    {
    }

    public DuplicateKeyException(string message)
        : base(message)
    {
    }

    public DuplicateKeyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public DuplicateKeyException(string? constraintName, string message, Exception innerException)
        : base(message, innerException)
        => ConstraintName = constraintName;

    /// <summary>Ten rang buoc/index bi vi pham (vd <c>ix_applications_job_id_candidate_id</c>), neu provider cho biet.</summary>
    public string? ConstraintName { get; }
}
